using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays game music as a random playlist, keeping song[0] reserved for the menu.
///
/// PLAYLIST BUG ROOT CAUSE:
///   Update() polled IsAnySongPlaying() every frame. During AudioManager's
///   3-second crossfade gap, all songs report isPlaying=false, so
///   PlayNextRandomSong() was called hundreds of times per second, spamming
///   ChangeSong() and causing erratic / no audible music.
///
/// FIX:
///   Removed Update() entirely. A coroutine waits for the current song's
///   clip length before queuing the next one, so there is exactly one
///   ChangeSong() call per track. The menu song (index 0) is never added
///   to the playlist and is never touched by this controller.
/// </summary>
public class MusicPlaylistController : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds to wait before starting the playlist after scene load.")]
    public float startDelay = 1f;

    [Tooltip("Avoid playing the same song twice in a row.")]
    public bool avoidImmediateRepeat = true;

    // ── State ─────────────────────────────────────────────────────────────────

    // Parallel lists: name for AudioManager, duration for coroutine timing
    private readonly List<string> _names     = new List<string>();
    private readonly List<float>  _durations = new List<float>();

    private int    _lastIndex = -1;
    private bool   _running   = false;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[MusicPlaylistController] AudioManager not found.");
            return;
        }

        BuildPlaylist();

        if (_names.Count == 0)
        {
            Debug.LogWarning("[MusicPlaylistController] Playlist is empty — no songs beyond index 0.");
            return;
        }

        StartCoroutine(PlaylistRoutine());
    }

    // ── Playlist building ─────────────────────────────────────────────────────

    private void BuildPlaylist()
    {
        _names.Clear();
        _durations.Clear();

        var songs = AudioManager.Instance.songs;
        if (songs == null) return;

        // Index 0 is the menu song — skip it intentionally
        for (int i = 1; i < songs.Length; i++)
        {
            var s = songs[i];
            if (s == null || string.IsNullOrEmpty(s.soundName)) continue;

            // Get clip duration for timing. Fall back to 3 min if clip is missing.
            float dur = (s.clip != null) ? s.clip.length : 180f;

            _names.Add(s.soundName);
            _durations.Add(dur);
        }
    }

    // ── Playlist coroutine ────────────────────────────────────────────────────

    private IEnumerator PlaylistRoutine()
    {
        _running = true;

        yield return new WaitForSeconds(startDelay);

        while (_running)
        {
            int index = PickNextIndex();
            _lastIndex = index;

            string songName = _names[index];
            float  duration = _durations[index];

            AudioManager.Instance.ChangeSong(songName);

            // Wait for the full song to play out (clip length + crossfade buffer)
            // The 3s crossfade in AudioManager is baked in; we add a small buffer.
            float crossfadeBuffer = 3.5f;
            yield return new WaitForSeconds(duration + crossfadeBuffer);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private int PickNextIndex()
    {
        if (_names.Count == 1) return 0;

        if (!avoidImmediateRepeat) 
            return Random.Range(0, _names.Count);

        int pick;
        do { pick = Random.Range(0, _names.Count); }
        while (pick == _lastIndex);
        return pick;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Stop the playlist (e.g. when returning to the main menu).</summary>
    public void Stop()
    {
        _running = false;
        StopAllCoroutines();
    }

    /// <summary>Skip the current song and immediately play the next random one.</summary>
    public void SkipToNext()
    {
        StopAllCoroutines();
        StartCoroutine(PlaylistRoutine());
    }
}