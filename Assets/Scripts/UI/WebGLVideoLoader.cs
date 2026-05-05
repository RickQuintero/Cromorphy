using UnityEngine;
using UnityEngine.Video;

public class WebGLVideoLoader : MonoBehaviour
{
    public VideoPlayer videoPlayer;

    void Start()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "intro.mp4");
        videoPlayer.url = path;

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.errorReceived += OnError;

        videoPlayer.Prepare();
    }

    void OnPrepared(VideoPlayer vp)
    {
        Debug.Log("Video prepared, playing now");
        vp.Play();
    }

    void OnError(VideoPlayer vp, string message)
    {
        Debug.LogError("Video error: " + message);
    }
}