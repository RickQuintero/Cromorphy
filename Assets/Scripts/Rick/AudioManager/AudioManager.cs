using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton AudioManager supporting both 2D and 3D spatial audio.
///
/// USAGE EXAMPLES:
///   AudioManager.Instance.PlaySong("Daysong");
///   AudioManager.Instance.PlaySong("Daysong", transform.position);
///   AudioManager.Instance.PlayEffect("Explosion", transform.position);
///   AudioManager.Instance.PlayEffect("UIClick");
///   AudioManager.Instance.ChangeSong("NightSong");
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────

    public static AudioManager Instance { get; private set; }

    [Header("Songs")]
    public SoundData[] songs;

    [Header("Effects")]
    public SoundData[] effects;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;
    public float voiceVolume = 0.4f;
    [Header("3D Effect Pool")]
    [Tooltip("Pre-warmed pool size for 3D one-shot effects. Grows automatically if needed.")]
    [SerializeField] private int initialPoolSize = 10;

    // ─────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────

    // Persistent child GOs for songs (key = soundName)
    private readonly Dictionary<string, AudioSource> _songSources   = new Dictionary<string, AudioSource>();
    // Persistent child GOs for 2D looping effects (key = soundName)
    private readonly Dictionary<string, AudioSource> _effectSources = new Dictionary<string, AudioSource>();
    // Fast lookup for SoundData
    private readonly Dictionary<string, SoundData>   _songData      = new Dictionary<string, SoundData>();
    private readonly Dictionary<string, SoundData>   _effectData    = new Dictionary<string, SoundData>();

    // Pool for one-shot 3D effects
    private readonly Queue<AudioSource> _pool = new Queue<AudioSource>();
    private Transform _poolContainer;

    // Fade state
    private string  _currentSong    = null;
    private string  _pendingSong     = null;
    private bool    _volumeOn        = true;
    private float   _currentVolume   = 0f;
    private Coroutine _fadeCoroutine = null;

    // ─────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        CreatePoolContainer();
        RegisterSounds(songs,   _songSources,   _songData,   "Songs");
        RegisterSounds(effects, _effectSources, _effectData, "Effects");
        WarmPool(initialPoolSize);
    }

    private void LateUpdate()
    {
        // Smooth music volume fade per frame (preserves existing feel)
        if (_currentSong == null) return;

        float target = _volumeOn ? musicVolume : 0f;
        _currentVolume = Mathf.Lerp(_currentVolume, target, 0.1f);

        if (_songSources.TryGetValue(_currentSong, out AudioSource src))
            src.volume = _currentVolume;
    }

    // ─────────────────────────────────────────────────────────────
    //  Initialisation Helpers
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a child GameObject for each SoundData, attaches an AudioSource,
    /// and registers it in the provided dictionaries.
    /// 3D one-shot effects skip persistent source creation — they'll use the pool.
    /// </summary>
    private void RegisterSounds(
        SoundData[]                    data,
        Dictionary<string, AudioSource> sources,
        Dictionary<string, SoundData>   lookup,
        string                          groupName)
    {
        if (data == null) return;

        var groupParent = new GameObject(groupName);
        groupParent.transform.SetParent(transform);

        foreach (SoundData s in data)
        {
            if (s == null || string.IsNullOrEmpty(s.soundName))
            {
                Debug.LogWarning($"[AudioManager] A null or unnamed SoundData found in {groupName}. Skipped.");
                continue;
            }
            if (lookup.ContainsKey(s.soundName))
            {
                Debug.LogWarning($"[AudioManager] Duplicate sound name \"{s.soundName}\" in {groupName}. Skipped.");
                continue;
            }

            lookup[s.soundName] = s;

            // 3D non-looping effects use the pool at play-time — no persistent source needed.
            if (s.use3D && !s.loop) continue;

            var child  = new GameObject(s.soundName);
            child.transform.SetParent(groupParent.transform);

            var source = child.AddComponent<AudioSource>();
            s.ApplyTo(source);

            sources[s.soundName] = source;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Pool Management
    // ─────────────────────────────────────────────────────────────

    private void CreatePoolContainer()
    {
        var go = new GameObject("Pool_3DEffects");
        go.transform.SetParent(transform);
        _poolContainer = go.transform;
    }

    private void WarmPool(int count)
    {
        for (int i = 0; i < count; i++)
            _pool.Enqueue(CreatePooledSource());
    }

    private AudioSource CreatePooledSource()
    {
        var go = new GameObject("PooledAudioSource");
        go.transform.SetParent(_poolContainer);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        go.SetActive(false);
        return source;
    }

    private AudioSource RentFromPool()
    {
        AudioSource source = _pool.Count > 0
            ? _pool.Dequeue()
            : CreatePooledSource(); // grow automatically

        source.gameObject.SetActive(true);
        return source;
    }

    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        source.transform.SetParent(_poolContainer);
        source.transform.localPosition = Vector3.zero;
        _pool.Enqueue(source);
    }

    // ─────────────────────────────────────────────────────────────
    //  Song API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Fades out the current song, then crossfades into the new one.</summary>
    public void ChangeSong(string name)
    {
        if (_currentSong == name) return;
        _pendingSong = name;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CrossfadeSong());
    }

    /// <summary>Play a song immediately (2D).</summary>
    public void PlaySong(string name) => PlaySong(name, Vector3.zero, false);

    /// <summary>Play a song at a world position (3D only applies if the SoundData has Use3D enabled).</summary>
    public void PlaySong(string name, Vector3 position) => PlaySong(name, position, true);

    /// <summary>Play a song following a transform (repositioned each frame is overkill for music — prefer PlaySong(name, transform.position)).</summary>
    public void PlaySong(string name, Transform follow) => PlaySong(name, follow.position, true);

    private void PlaySong(string name, Vector3 position, bool hasPosition)
    {
        if (_currentSong == name) return;

        StopSong(_currentSong);

        if (!_songData.TryGetValue(name, out SoundData data))
        {
            Debug.LogWarning($"[AudioManager] Song \"{name}\" not found.");
            return;
        }

        _currentSong  = name;
        _currentVolume = 0f;
        _volumeOn      = true;

        if (!_songSources.TryGetValue(name, out AudioSource source))
        {
            Debug.LogWarning($"[AudioManager] No AudioSource registered for song \"{name}\". Was it marked Use3D+non-loop?");
            return;
        }

        if (hasPosition && data.use3D)
            source.transform.position = position;

        source.Play();
    }

    public void StopSong(string name)
    {
        if (name == null) return;
        if (_songSources.TryGetValue(name, out AudioSource source))
            source.Stop();
    }

    private IEnumerator CrossfadeSong()
    {
        _volumeOn = false;
        yield return new WaitForSeconds(3f);  // matches original fade duration
        PlaySong(_pendingSong);
        _fadeCoroutine = null;
    }

    // ─────────────────────────────────────────────────────────────
    //  Effect API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Play a one-shot or looping effect in 2D.</summary>
    public void PlayEffect(string name) => PlayEffectInternal(name, Vector3.zero, null, false);

    /// <summary>Play an effect at a world position. Uses 3D if SoundData.use3D is true.</summary>
    public void PlayEffect(string name, Vector3 position) => PlayEffectInternal(name, position, null, true);

    /// <summary>Play an effect attached to a transform. Keeps it positioned on moving objects.</summary>
    public void PlayEffect(string name, Transform follow) => PlayEffectInternal(name, follow.position, follow, true);

    private void PlayEffectInternal(string name, Vector3 position, Transform follow, bool hasPosition)
    {
        if (!_effectData.TryGetValue(name, out SoundData data))
        {
            Debug.LogWarning($"[AudioManager] Effect \"{name}\" not found.");
            return;
        }

        bool spatial = data.use3D && hasPosition;

        if (spatial && !data.loop)
        {
            // ── One-shot 3D: use pool ──────────────────────────────────
            AudioSource pooled = RentFromPool();
            data.ApplyTo(pooled);
            pooled.transform.position = position;
            pooled.Play();
            StartCoroutine(ReturnToPoolWhenDone(pooled, data.clip.length / Mathf.Abs(data.pitch)));
        }
        else if (spatial && data.loop)
        {
            // ── Looping 3D: use persistent source, reposition it ──────
            if (_effectSources.TryGetValue(name, out AudioSource src))
            {
                src.transform.position = position;
                if (!src.isPlaying) src.Play();
            }
        }
        else
        {
            // ── 2D (persistent source) ────────────────────────────────
            if (_effectSources.TryGetValue(name, out AudioSource src))
            {
                if (data.loop)
                {
                    if (!src.isPlaying) src.Play();
                }
                else
                {
                    src.PlayOneShot(data.clip, data.volume);
                }
            }
        }
    }

    private IEnumerator ReturnToPoolWhenDone(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration + 0.1f);
        ReturnToPool(source);
    }

    public void StopEffect(string name)
    {
        if (_effectSources.TryGetValue(name, out AudioSource source))
            source.Stop();
    }

    // ─────────────────────────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────────────────────────

    /// <summary>Override pitch on a playing effect at runtime (e.g. for dynamic music systems).</summary>
    public void SetEffectPitch(string name, float pitch)
    {
        if (_effectSources.TryGetValue(name, out AudioSource source))
            source.pitch = pitch;
        else
            Debug.LogWarning($"[AudioManager] Cannot set pitch — Effect \"{name}\" not found or is 3D one-shot.");
    }

    /// <summary>Check if a song is currently playing.</summary>
    public bool IsSongPlaying(string name) =>
        _songSources.TryGetValue(name, out AudioSource src) && src.isPlaying;

    /// <summary>Check if an effect is currently playing (looping/persistent sources only).</summary>
    public bool IsEffectPlaying(string name) =>
        _effectSources.TryGetValue(name, out AudioSource src) && src.isPlaying;

    public string CurrentSong => _currentSong;
}
