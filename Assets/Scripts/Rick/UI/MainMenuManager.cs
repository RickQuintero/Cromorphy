using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Escenas")]
    [Tooltip("El nombre exacto de la escena que se cargará al pulsar Jugar.")]
    public string gameSceneName = "Zone_01";

    [Header("Transición (Fade)")]
    [Tooltip("Imagen negra (o del color que prefieras) que cubre toda la pantalla.")]
    public Image fadeOverlay;
    [Tooltip("Tiempo en segundos que tarda en oscurecerse la pantalla.")]
    public float fadeDuration = 1.5f;

    [Header("Panel Cargar Partida")]
    [Tooltip("Panel que contiene los slots de partidas guardadas.")]
    public GameObject loadGamePanel;

    private bool _isTransitioning = false;

    private void Start()
    {
        Debug.Log("MainMenuManager Start() ejecutado.");
        // Si hay un overlay, nos aseguramos de que empiece transparente y sin bloquear clics.
        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.raycastTarget = false;
        }

        // El panel de carga empieza cerrado
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);

        // Limpiar el slot pendiente de carga (por si quedó de una sesión anterior)
        PlayerPrefs.DeleteKey("pending_load_slot");
    }

    public void PlayGame()
    {
        Debug.Log("¡Botón JUGAR clicado!");
        Debug.Log("Intentando cargar la escena con nombre exacto: '" + gameSceneName + "'");

        if (_isTransitioning) 
        {
            Debug.Log("Ignorando clic porque ya está en transición.");
            return;
        }
        _isTransitioning = true;

        if (fadeOverlay != null)
        {
            Debug.Log("Iniciando Corrutina de FadeOut...");
            StartCoroutine(FadeAndLoad());
        }
        else
        {
            Debug.Log("Cargando escena directamente (LoadScene sincrónico)...");
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
        if (_isTransitioning) return;

        Debug.Log("Saliendo del juego...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        Application.Quit();
    }

    /// <summary>Abre el panel de slots de partidas guardadas.</summary>
    public void OpenLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(true);
    }

    /// <summary>Cierra el panel de slots.</summary>
    public void CloseLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
    }

    private IEnumerator FadeAndLoad()
    {
        // Hacer que la imagen intercepte clics para que el usuario no pueda seguir tocando botones.
        fadeOverlay.raycastTarget = true;
        
        float timer = 0f;
        Color color = fadeOverlay.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeOverlay.color = color;

        // Cargar la escena
        SceneManager.LoadScene(gameSceneName);
    }
}
