using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public void SetVolumeMaster(float volume)
    {
        audioMixer.SetFloat("Master",volume);
    }
    public void SetVolumeMusic(float volume)
    {
        audioMixer.SetFloat("Music",volume);
    }
    public void SetVolumeEffects(float volume)
    {
        audioMixer.SetFloat("Fx",volume);
    }
    public void SetQuality(int index)
    {
        //QualitySettings.SetQualityLevel(index);
    }
    
}
