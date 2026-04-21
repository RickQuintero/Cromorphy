using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float hoverScale = 1.15f;    // Cuánto crece al poner el ratón encima
    public float clickScale = 0.95f;    // Cuánto se encoge al hacer clic
    public float lerpSpeed = 15f;       // Velocidad de interpolación

    private Vector3 _originalScale;
    private Vector3 _targetScale;
    
    private void Start()
    {
        _originalScale = transform.localScale;
        _targetScale   = _originalScale;
    }

    private void Update()
    {
        // En cada frame suavizamos la escala hacia nuestro objetivo
        // Usamos unscaledDeltaTime para que siga funcionando incluso si hiciste un Time.timeScale = 0 en tu juego.
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.unscaledDeltaTime * lerpSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _targetScale = _originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _originalScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Al soltar el clic, el ratón comúnmente sigue estando encima del botón, así que pasa a estado Hover
        _targetScale = _originalScale * hoverScale;
    }
}
