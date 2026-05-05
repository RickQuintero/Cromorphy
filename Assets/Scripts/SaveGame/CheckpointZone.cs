using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointZone : MonoBehaviour
{
    [Header("Identification")]
    [Tooltip("Unique index for this checkpoint in the scene. Start at 0.")]
    public int checkpointIndex = 0;

    [Tooltip("Tag used to identify the player.")]
    public string playerTag = "Player";

    [Header("Visual (optional)")]
    [Tooltip("Object activated when this checkpoint is reached (particle, flag, light, etc.)")]
    public GameObject activatedVisual;

    private bool _activated;

    private void Start()
    {
        if (CheckpointManager.Instance != null &&
            CheckpointManager.Instance.LastCheckpointIndex == checkpointIndex)
            SetActivated();
    }

    private void OnTriggerEnter(Collider other)     => TryActivate(other.gameObject);
    private void OnTriggerEnter2D(Collider2D other) => TryActivate(other.gameObject);
    private void OnTriggerExit2D(Collider2D other)  => TryAutoSave(other.gameObject);

    private void TryActivate(GameObject other)
    {
        if (_activated || !other.CompareTag(playerTag)) return;

        SetActivated();
        CheckpointManager.Instance?.RegisterCheckpoint(this);
    }

    private void TryAutoSave(GameObject other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (CheckpointManager.Instance?.LastCheckpointIndex != checkpointIndex) return;

        int slot = SaveSystem.GetActiveSlot();
        if (slot < 0) return;

        var data = new SaveSystem.SaveData
        {
            exists           = true,
            checkpointIndex  = checkpointIndex,
            sceneName        = SceneManager.GetActiveScene().name,
            playerPosition   = transform.position,
            isDay            = DayLightController.Instance?.IsDay ?? true,
            numberOfCycles   = ScoreManager.Instance?.numberOfCycles ?? 0,
            maxJumpLevel     = ScoreManager.Instance?.MaxJumpLevel ?? 1,
            maxTongueRadius  = ScoreManager.Instance?.MaxTongueRadius ?? 3,
            maxCamuflajeTime = ScoreManager.Instance?.MaxCamuflajeTime ?? 5,
        };

        SaveSystem.Save(slot, data);
        Debug.Log($"[CheckpointZone] Auto-saved slot {slot} on exit — checkpoint {checkpointIndex}");
    }

    private void SetActivated()
    {
        _activated = true;
        if (activatedVisual != null) activatedVisual.SetActive(true);
    }
}
