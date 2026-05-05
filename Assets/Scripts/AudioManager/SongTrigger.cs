using UnityEngine;

/// <summary>
/// 2D trigger zone that crossfades to a new song when the Player enters.
/// </summary>
public class SongTrigger : MonoBehaviour
{
    [Tooltip("The soundName of the song to transition to.")]
    public string songToChangeTo = "";
    public bool selfDestruct = false; 
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (AudioManager.Instance.CurrentSong == songToChangeTo) return;
            AudioManager.Instance.ChangeSong(songToChangeTo);
        if (selfDestruct)
            Destroy(gameObject);
    }
}
