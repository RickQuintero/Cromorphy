using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    public int     LastCheckpointIndex    { get; private set; } = -1;
    public Vector3 LastCheckpointPosition { get; private set; }
    public bool    CanSave => LastCheckpointIndex >= 0;

    public event System.Action OnCheckpointReached;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterCheckpoint(CheckpointZone zone)
    {
        if (zone.checkpointIndex <= LastCheckpointIndex) return;

        LastCheckpointIndex    = zone.checkpointIndex;
        LastCheckpointPosition = zone.transform.position;

        OnCheckpointReached?.Invoke();
        Debug.Log($"[CheckpointManager] Active checkpoint → {LastCheckpointIndex}");
    }

    // Call this after loading a save, before moving the player to the saved position.
    public void RestoreFromSave(SaveSystem.SaveData data)
    {
        LastCheckpointIndex    = data.checkpointIndex;
        LastCheckpointPosition = data.playerPosition;
    }
}
