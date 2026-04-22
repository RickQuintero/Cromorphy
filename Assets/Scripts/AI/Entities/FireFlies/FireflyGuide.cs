using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Luciérnaga individual que parpadea en código Morse
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FireflyGuide : MonoBehaviour {
    
    // Diccionario de código Morse integrado
    private static readonly Dictionary<char, string> morseCode = new Dictionary<char, string>() {
        {'A', ".-"}, {'B', "-..."}, {'C', "-.-."}, {'D', "-.."}, {'E', "."}, {'F', "..-."},
        {'G', "--."}, {'H', "...."}, {'I', ".."}, {'J', ".---"}, {'K', "-.-"}, {'L', ".-.."},
        {'M', "--"}, {'N', "-."}, {'O', "---"}, {'P', ".--."}, {'Q', "--.-"}, {'R', ".-."},
        {'S', "..."}, {'T', "-"}, {'U', "..-"}, {'V', "...-"}, {'W', ".--"}, {'X', "-..-"},
        {'Y', "-.--"}, {'Z', "--.."}, 
        {'0', "-----"}, {'1', ".----"}, {'2', "..---"}, {'3', "...--"}, {'4', "....-"},
        {'5', "....."}, {'6', "-...."}, {'7', "--..."}, {'8', "---.."}, {'9', "----."},
        {' ', "/"}
    };
    
    [Header("Visual Settings")]
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float minIntensity = 0.1f;
    
    [Header("Movement")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private Vector2 randomOffset;
    
    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private string morseMessage = "";
    private int currentSymbolIndex = 0;
    private bool isBlinking = false;
    private Vector3 startPosition;
    private float timeOffset;

    void Awake() {
        spriteRenderer = GetComponent<SpriteRenderer>();
        light2D = GetComponent<Light2D>();
        
        // Configurar luz
        if (light2D != null) {
            light2D.color = glowColor;
            light2D.intensity = minIntensity;
        }
        
        // Offset aleatorio para movimiento
        timeOffset = Random.Range(0f, 100f);
        randomOffset = Random.insideUnitCircle * 0.5f;
    }

    void Start() {
        startPosition = transform.localPosition;
    }

    void Update() {
        // Movimiento flotante
        FloatMovement();
    }

    /// <summary>
    /// Establece el mensaje en código Morse que parpadeará
    /// </summary>
    public void SetMorseMessage(string message) {
        morseMessage = TextToMorse(message);
        
        if (!isBlinking && !string.IsNullOrEmpty(morseMessage)) {
            StartCoroutine(BlinkMorseCode());
        }
    }
    
    /// <summary>
    /// Convierte texto a código Morse
    /// </summary>
    private static string TextToMorse(string text) {
        text = text.ToUpper();
        string morse = "";
        
        foreach (char c in text) {
            if (morseCode.ContainsKey(c)) {
                morse += morseCode[c] + " ";
            }
        }
        
        return morse.Trim();
    }

    /// <summary>
    /// Movimiento flotante suave
    /// </summary>
    private void FloatMovement() {
        float time = Time.time * floatSpeed + timeOffset;
        Vector3 offset = new Vector3(
            Mathf.Sin(time) * floatAmplitude + randomOffset.x,
            Mathf.Cos(time * 1.3f) * floatAmplitude + randomOffset.y,
            0f
        );
        
        transform.localPosition = startPosition + offset;
    }
    
    /// <summary>
    /// Obtiene la duración de un símbolo Morse en segundos
    /// </summary>
    private static float GetSymbolDuration(char symbol, float dotDuration = 0.2f) {
        switch (symbol) {
            case '.': return dotDuration;              // Punto
            case '-': return dotDuration * 3f;         // Raya (3 veces el punto)
            case ' ': return dotDuration * 3f;         // Espacio entre letras
            case '/': return dotDuration * 7f;         // Espacio entre palabras
            default: return dotDuration;
        }
    }

    /// <summary>
    /// Corrutina que parpadea el código Morse
    /// </summary>
    private IEnumerator BlinkMorseCode() {
        isBlinking = true;
        
        while (true) {
            if (string.IsNullOrEmpty(morseMessage)) {
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Recorrer cada símbolo del mensaje Morse
            foreach (char symbol in morseMessage) {
                if (symbol == '.' || symbol == '-') {
                    // Encender
                    yield return StartCoroutine(SetGlow(true, 0.1f));
                    
                    // Mantener encendido según el símbolo
                    float duration = GetSymbolDuration(symbol);
                    yield return new WaitForSeconds(duration);
                    
                    // Apagar
                    yield return StartCoroutine(SetGlow(false, 0.1f));
                    
                    // Pausa entre símbolos
                    yield return new WaitForSeconds(0.2f);
                }
                else if (symbol == ' ') {
                    // Pausa entre letras
                    yield return new WaitForSeconds(0.6f);
                }
                else if (symbol == '/') {
                    // Pausa entre palabras
                    yield return new WaitForSeconds(1.4f);
                }
            }
            
            // Pausa antes de repetir el mensaje
            yield return new WaitForSeconds(2f);
        }
    }

    /// <summary>
    /// Enciende o apaga el brillo de la luciérnaga
    /// </summary>
    private IEnumerator SetGlow(bool on, float duration) {
        float startIntensity = light2D != null ? light2D.intensity : 0f;
        float targetIntensity = on ? maxIntensity : minIntensity;
        
        Color startColor = spriteRenderer.color;
        Color targetColor = on ? glowColor : new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
        
        float elapsed = 0f;
        
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (light2D != null) {
                light2D.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            }
            
            spriteRenderer.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        if (light2D != null) {
            light2D.intensity = targetIntensity;
        }
        spriteRenderer.color = targetColor;
    }

    /// <summary>
    /// Detiene el parpadeo
    /// </summary>
    public void StopBlinking() {
        StopAllCoroutines();
        isBlinking = false;
        
        if (light2D != null) {
            light2D.intensity = minIntensity;
        }
    }
}
