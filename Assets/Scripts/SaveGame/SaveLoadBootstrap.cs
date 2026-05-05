using UnityEngine;

// Runs after CheckpointManager (order 0) so Instance is ready, but before
// CheckpointZone.Start() reads LastCheckpointIndex.
[DefaultExecutionOrder(50)]
public class SaveLoadBootstrap : MonoBehaviour
{
    private SaveSystem.SaveData _loadedData;

    private void Awake()
    {
        if (!PlayerPrefs.HasKey(SaveSystem.PENDING_LOAD_KEY)) return;

        int slot = PlayerPrefs.GetInt(SaveSystem.PENDING_LOAD_KEY);
        PlayerPrefs.DeleteKey(SaveSystem.PENDING_LOAD_KEY);
        PlayerPrefs.Save();

        SaveSystem.SaveData data = SaveSystem.Load(slot);
        if (data == null) return;

        SaveSystem.SetActiveSlot(slot);
        CheckpointManager.Instance?.RestoreFromSave(data);
        TeleportPlayer(data.playerPosition);
        RestoreScore(data);

        _loadedData = data;
        Debug.Log($"[SaveLoadBootstrap] Loaded slot {slot} — checkpoint {data.checkpointIndex} at {data.playerPosition}");
    }

    // DayLightController.Start() (order 0) runs before this (order 50),
    // so we can safely override the initial day/night state here.
    private void Start()
    {
        if (_loadedData == null) return;
        DayLightController.Instance?.SetDayNightImmediate(_loadedData.isDay);
    }

    private static void TeleportPlayer(Vector3 position)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        player.transform.position = position;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }

        var controller = player.GetComponent<PlayerController2D>();
        if (controller?.bodyRigidbodies == null) return;

        foreach (var body in controller.bodyRigidbodies)
        {
            if (body == null) continue;
            body.position        = position;
            body.linearVelocity  = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private static void RestoreScore(SaveSystem.SaveData data)
    {
        var score = ScoreManager.Instance;
        if (score == null) return;

        score.numberOfCycles   = data.numberOfCycles;
        score.MaxJumpLevel     = data.maxJumpLevel;
        score.MaxTongueRadius  = data.maxTongueRadius;
        score.MaxCamuflajeTime = data.maxCamuflajeTime;
    }
}
