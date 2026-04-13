using UnityEngine;

/// <summary>
/// Trigger zone that crossfades to a new song when the Player enters.
/// Optionally pass this trigger's position if the target song uses 3D audio.
/// </summary>
public class SongTrigger : MonoBehaviour
{
    [Tooltip("The soundName of the song to transition to.")]
    public string songToChangeTo = "";
    public bool selfDestruct = false; 
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (AudioManager.Instance.CurrentSong == songToChangeTo) return;
            AudioManager.Instance.ChangeSong(songToChangeTo);
        if (selfDestruct)
            Destroy(gameObject);
    }
}
