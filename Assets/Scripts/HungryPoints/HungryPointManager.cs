using UnityEngine;

public class HungryPointManager : MonoBehaviour
{
    public static HungryPointManager Instance { get; private set; }

    [Tooltip("Maximum number of hungry points the player can accumulate.")]
    public int maxHungryPoints = 10;

    public int currentHungryPoints { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void AddPoint()
    {
        currentHungryPoints = Mathf.Min(currentHungryPoints + 1, maxHungryPoints);
        AudioManager.Instance.PlayEffect("COLLECTABLE");
    }

    public void RemovePoint(int amount)
    {
        currentHungryPoints = Mathf.Max(currentHungryPoints - amount, 0);
    }
}
