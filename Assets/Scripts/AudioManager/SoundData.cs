using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// ScriptableObject that holds all configuration for a single sound.
/// Create via: Assets > Create > Audio > Sound Data
/// </summary>
[CreateAssetMenu(fileName = "NewSound", menuName = "Audio/Sound Data")]
public class SoundData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique key used to play this sound from code, e.g. \"Daysong\" or \"ExplosionFX\"")]
    public string soundName;

    [Header("Clip & Mixer")]
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;

    [Header("Playback")]
    [Range(0f, 1f)]  public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch  = 1f;
    public bool loop = false;

    [Header("3D Spatial Audio")]
    [Tooltip("Enable to make this sound positional in 3D space.")]
    public bool use3D = false;

    [Tooltip("Distance at which the sound starts attenuating. Has no effect when Use3D is false.")]
    public float minDistance = 1f;

    [Tooltip("Distance at which the sound reaches minimum volume. Has no effect when Use3D is false.")]
    public float maxDistance = 50f;

    [Tooltip("Curve shape for how the sound attenuates with distance. Has no effect when Use3D is false.")]
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    /// <summary>
    /// Applies all settings from this ScriptableObject onto a live AudioSource.
    /// Call this whenever you attach a new AudioSource to a pooled or dedicated object.
    /// </summary>
    public void ApplyTo(AudioSource source)
    {
        source.clip                  = clip;
        source.outputAudioMixerGroup = mixerGroup;
        source.volume                = volume;
        source.pitch                 = pitch;
        source.loop                  = loop;
        source.spatialBlend          = use3D ? 1f : 0f;
        source.minDistance           = minDistance;
        source.maxDistance           = maxDistance;
        source.rolloffMode           = rolloffMode;
        source.playOnAwake           = false;
    }
}
