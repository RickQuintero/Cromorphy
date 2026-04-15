using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ArtSceneManager : MonoBehaviour
{
    public static ArtSceneManager Instance { get; private set; }

    [Header("Loading UI Elements")]
    public GameObject Loading_UI;
    public Image Loading_fillImage;
    public Slider Loading_Slider;
    public TextMeshProUGUI Loading_text;

    [Header("Fade System Settings")]
    public Image fadeImage; // This will be the UI Image for fading
    public float fadeSpeed = 1.0f; // Speed of fading
    public bool fadingSystemEnabled = true; // Overall toggle for the fading system
    private bool _isFadingIn = false; // Internal flag for fade direction

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // Ensure the Loading UI is initially off
            Loading_UI.SetActive(false);
            // Ensure fadeImage is off or fully transparent at start if you want an immediate game view
            if (fadeImage != null)
            {
                fadeImage.gameObject.SetActive(true);
                StartFadeOut();
                //fadeImage.color = new Color(fadeImage.color.r, fadeImage.color.g, fadeImage.color.b, 0);
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void LateUpdate()
    {
        if (!fadingSystemEnabled || fadeImage == null) return;
        Color newColor = fadeImage.color;
        if (_isFadingIn)
        {
            // Fade to black (alpha reaches 1)
            if (newColor.a < 1)
            {
                newColor.a += fadeSpeed * Time.deltaTime;
                fadeImage.color = newColor;
            }
        }
        else
        {
            // Fade to transparent (alpha reaches 0)
            if (newColor.a > 0)
            {
                newColor.a -= fadeSpeed * Time.deltaTime;
                fadeImage.color = newColor;
            }
            else
            {
                // Ensure it's fully transparent and disable if needed
                newColor.a = 0;
                fadeImage.color = newColor;
                fadeImage.gameObject.SetActive(false); // Optional: disable the GameObject if fully transparent
            }
        }
    }

    IEnumerator LoadAsynchronously(int lvload)
    {
        Loading_UI.SetActive(true);
        // First, ensure we start fading in before loading
        if (fadingSystemEnabled)
        {
            
            StartFadeIn();
            // Wait until the fade is complete before starting the load operation
            yield return new WaitUntil(() => fadeImage.color.a >= 0.99f); // Wait until almost fully black
        }

        // Activate loading UI once screen is black, if it's not already
        if (Loading_UI != null)
        {
            Loading_UI.SetActive(true);
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(lvload);
        operation.allowSceneActivation = false; // Prevent scene from activating until fade out is ready

        while (!operation.isDone)
        {
            float progressText = Mathf.Round(operation.progress * 100);
            Loading_text.text = progressText.ToString() + "%";
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (Loading_Slider != null)
            {
                Loading_Slider.value = progress;
            }
            if (Loading_fillImage != null)
            Loading_fillImage.fillAmount = progress;

            // When loading is almost done, allow scene activation and prepare to fade out
            if (operation.progress >= 0.9f)
            {
                Loading_UI.SetActive(false); // Hide the loading progress UI
                operation.allowSceneActivation = true; // Allow the scene to activate
                if (fadingSystemEnabled)
                {
                    StartFadeOut(); // Start fading out as the new scene loads
                }
            }
            yield return null;
        }
        // After the scene is fully loaded and activated, and fade out has begun/completed
        if (operation.isDone)
        {
            StopAllCoroutines(); // Stop any lingering coroutines if needed
        }
    }

    public void RestartLevel()
    {
        LoadSceneByNumber(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        LoadSceneByNumber(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void LoadSceneByNumber(int lvload)
    {
        // We'll activate the Loading_UI and start the fade within LoadAsynchronously
        StartCoroutine(LoadAsynchronously(lvload));
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    // New public methods for controlling the fade
    public void StartFadeIn()
    {
        if (fadeImage != null && fadingSystemEnabled)
        {
            fadeImage.gameObject.SetActive(true); // Ensure the fade image is active
            _isFadingIn = true;
        }
    }

    public void StartFadeOut()
    {
        if (fadeImage != null && fadingSystemEnabled)
        {
            fadeImage.gameObject.SetActive(true); // Ensure the fade image is active
            _isFadingIn = false;
        }
    }
    
}