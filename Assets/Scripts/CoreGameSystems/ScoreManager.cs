using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
    public int numberOfCycles = 0;
    public int MaxJumpLevel = 1;
    public int MaxTongueRadius = 3;
    public int MaxCamuflajeTime = 5;
}
