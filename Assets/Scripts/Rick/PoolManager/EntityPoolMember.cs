using UnityEngine;

/// <summary>
/// Attached automatically by EntityPoolManager to every pooled object.
/// Stores the pool index so the object can return itself without knowing
/// any pool internals.
///
/// This is an updated version of the original EntityPoolMember.cs that
/// adds the public ReturnToPool() helper called by UnitBase on death.
/// </summary>
public class EntityPoolMember : MonoBehaviour
{
    public int  EntityIndex { get; private set; }
    public bool IsEnemy     { get; private set; }

    /// <summary>Called once by EntityPoolManager after Instantiate.</summary>
    public void Initialise(int index, bool isEnemy)
    {
        EntityIndex = index;
        IsEnemy     = isEnemy;
    }

    /// <summary>Return this object to its pool. Safe to call from any script.</summary>
    public void ReturnToPool()
    {
        if (EntityPoolManager.Instance != null)
            EntityPoolManager.Instance.ReturnToPool(gameObject, EntityIndex);
        else
            gameObject.SetActive(false);   // Fallback if pool is gone (scene unloading)
    }
}
