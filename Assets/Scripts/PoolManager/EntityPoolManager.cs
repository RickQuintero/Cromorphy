using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// Per-entity-type configuration — visible and editable in the Inspector
// ---------------------------------------------------------------------------
[System.Serializable]
public class EntityEntry
{
    public GameObject prefab;

    [Tooltip("Is this entity an enemy? Uncheck for allies, NPCs, neutrals, etc.")]
    public bool isEnemy = true;

    [Tooltip("Maximum number of this entity type alive at the same time.")]
    public int maxAlive = 5;

    [Tooltip("How many instances to create in the pool on startup.")]
    public int prewarm = 3;

    // Runtime — not shown in Inspector
    [System.NonSerialized] public int activeCount = 0;

    public bool HasRoom => activeCount < maxAlive;
}

// ---------------------------------------------------------------------------
// Central pool — one instance per scene, never disabled
// ---------------------------------------------------------------------------
public class EntityPoolManager : MonoBehaviour
{
    public static EntityPoolManager Instance { get; private set; }

    [Header("Pool Settings")]
    public EntityEntry[] entities;

    [Header("Global Cap")]
    [Tooltip("Hard ceiling across ALL entity types combined.")]
    public int maxEntitiesAlive = 20;

    private Queue<GameObject>[] _pools;
    private Transform[]         _poolContainers; // one child GameObject per entity type
    private int _activeEntityCount = 0;

    public int  ActiveEntityCount => _activeEntityCount;
    public bool HasRoom           => _activeEntityCount < maxEntitiesAlive;
    public int  EntityTypeCount   => entities.Length;

    // -------------------------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        InitialisePools();
    }

    // -------------------------------------------------------------------------
    // Pool management
    // -------------------------------------------------------------------------

    void InitialisePools()
    {
        _pools          = new Queue<GameObject>[entities.Length];
        _poolContainers = new Transform[entities.Length];

        for (int i = 0; i < entities.Length; i++)
        {
            string containerName = entities[i].prefab != null
                                   ? entities[i].prefab.name + "Pool"
                                   : "EntityPool_" + i;

            GameObject container = new GameObject(containerName);
            container.transform.SetParent(transform);
            _poolContainers[i] = container.transform;

            _pools[i] = new Queue<GameObject>();
            for (int j = 0; j < entities[i].prewarm; j++)
                _pools[i].Enqueue(CreateInstance(i));
        }
    }

    GameObject CreateInstance(int index)
    {
        GameObject obj = Instantiate(entities[index].prefab, _poolContainers[index]);
        obj.SetActive(false);

        EntityPoolMember member = obj.GetComponent<EntityPoolMember>()
                               ?? obj.AddComponent<EntityPoolMember>();
        member.Initialise(index, entities[index].isEnemy);

        return obj;
    }

    // -------------------------------------------------------------------------
    // Public API used by spawners
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns a specific entity type by index.
    /// Respects both the global cap and the per-type cap.
    /// Returns the instance, or null if either cap is reached.
    /// </summary>
    public GameObject Spawn(int entityIndex, Vector3 position, Quaternion rotation)
    {
        if (!HasRoom) return null;

        if (entityIndex < 0 || entityIndex >= entities.Length)
        {
            Debug.LogWarning($"[EntityPoolManager] Entity index {entityIndex} is out of range.");
            return null;
        }

        EntityEntry entry = entities[entityIndex];

        if (!entry.HasRoom)
        {
            Debug.Log($"[EntityPoolManager] Per-type cap reached for '{entry.prefab.name}'.");
            return null;
        }

        if (_pools[entityIndex].Count == 0)
            _pools[entityIndex].Enqueue(CreateInstance(entityIndex));

        GameObject entity = _pools[entityIndex].Dequeue();

        entity.transform.SetPositionAndRotation(position, rotation);
        entity.SetActive(true);

        entry.activeCount++;
        _activeEntityCount++;
        return entity;
    }

    /// <summary>Called by EntityPoolMember when the entity is done.</summary>
    public void ReturnToPool(GameObject entity, int entityIndex)
    {
        entity.SetActive(false);
        entity.transform.SetParent(_poolContainers[entityIndex]);
        _pools[entityIndex].Enqueue(entity);

        entities[entityIndex].activeCount--;
        _activeEntityCount--;
    }

    /// <summary>Returns how many of a specific type are still available to spawn.</summary>
    public int AvailableOfType(int entityIndex)
    {
        if (entityIndex < 0 || entityIndex >= entities.Length) return 0;
        return entities[entityIndex].maxAlive - entities[entityIndex].activeCount;
    }

    // -------------------------------------------------------------------------
    // Convenience queries — filter active counts by category
    // -------------------------------------------------------------------------

    /// <summary>Total active entities matching the given isEnemy value.</summary>
    public int ActiveCountByType(bool isEnemy)
    {
        int count = 0;
        for (int i = 0; i < entities.Length; i++)
            if (entities[i].isEnemy == isEnemy)
                count += entities[i].activeCount;
        return count;
    }

    /// <summary>True if at least one entry of the requested category has room to spawn.</summary>
    public bool HasRoomForType(bool isEnemy)
    {
        if (!HasRoom) return false;
        for (int i = 0; i < entities.Length; i++)
            if (entities[i].isEnemy == isEnemy && entities[i].HasRoom)
                return true;
        return false;
    }
}
