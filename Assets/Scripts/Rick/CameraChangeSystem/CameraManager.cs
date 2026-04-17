using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private List<Camera> cameras;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }
    private void Start()
    {
        CountChildren();
        ResetAllCameras();
    }
    private void CountChildren()
    {
        cameras.Clear();
        foreach (Transform child in transform)
        {
            Camera cam = child.GetComponent<Camera>();
            if (cam != null)
                cameras.Add(cam);
        }

    }
    /// <summary>
    /// Disables all cameras, then enables the target one.
    /// </summary>
    public void SetActiveCamera(Camera target)
    {
        ResetAllCameras();
        Debug.Log($"Activating camera: {target.name}");
        target.enabled = true;
    }

    /// <summary>
    /// Fallback: disables every registered camera.
    /// </summary>
    public void ResetAllCameras()
    {
        foreach (Camera cam in cameras)
        {
            if (cam != null)
                cam.enabled = false;
        }
    }
}
