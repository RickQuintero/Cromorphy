using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArtSceneManager : MonoBehaviour
{
    public static ArtSceneManager Instance { get; private set; }

    [Header("Scene Transition")]
    public GameObject canvasTransitionScene;
    [SerializeField] private float sceneTransitionDuration = 3f;

    [Header("Cycle Transition")]
    public GameObject canvasTransitionCycle;
    [SerializeField] private float cycleTransitionDuration = 3f;

    private Animator _sceneAnimator;
    private Animator _cycleAnimator;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _sceneAnimator = canvasTransitionScene.GetComponent<Animator>();
            _cycleAnimator = canvasTransitionCycle.GetComponent<Animator>();
            canvasTransitionScene.SetActive(false);
            canvasTransitionCycle.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Scene Transitions (called by buttons) ──────────────────────────────

    public void LoadSceneByNumber(int index) => StartCoroutine(SceneTransition(index));
    public void RestartLevel() => LoadSceneByNumber(SceneManager.GetActiveScene().buildIndex);
    public void NextLevel() => LoadSceneByNumber(SceneManager.GetActiveScene().buildIndex + 1);
    public void LoadMainMenu() => LoadSceneByNumber(0);

    private IEnumerator SceneTransition(int sceneIndex)
    {
        canvasTransitionScene.SetActive(true);
        _sceneAnimator.SetBool("FadeIn", true);
        yield return new WaitForSeconds(sceneTransitionDuration);

        SceneManager.LoadScene(sceneIndex);

        _sceneAnimator.SetBool("FadeIn", false);
        yield return new WaitForSeconds(sceneTransitionDuration);
        canvasTransitionScene.SetActive(false);
    }

    // ── Cycle Transitions (called by Cycle_Trigger) ────────────────────────

    public void TriggerCycleTransition() => StartCoroutine(CycleTransition());

    private IEnumerator CycleTransition()
    {
        // IsDay false = night; set the param to the state we are transitioning INTO
        bool targetIsDay = !DayLightController.Instance.IsDay;
        canvasTransitionCycle.SetActive(true);
        _cycleAnimator.SetBool("IsDay", targetIsDay);

        yield return new WaitForSeconds(cycleTransitionDuration);

        DayLightController.Instance.TriggerCycle();
        canvasTransitionCycle.SetActive(false);
    }
}
