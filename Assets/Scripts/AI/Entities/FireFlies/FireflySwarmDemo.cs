using UnityEngine;

/// <summary>
/// Script de demostración para probar el sistema de luciérnagas
/// Adjunta este script a tu jugador para ver mensajes de debug
/// </summary>
public class FireflySwarmDemo : MonoBehaviour {
    
    [Header("Referencias")]
    [SerializeField] private FireflySwarm swarm;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;
    
    private Transform currentTarget;

    void Start() {
        // Buscar el enjambre si no está asignado
        if (swarm == null) {
            swarm = FindObjectOfType<FireflySwarm>();
        }
        
        if (swarm != null && showDebugInfo) {
            Debug.Log("[FireflyDemo] Sistema de luciérnagas encontrado y listo.");
        }
    }

    void Update() {
        // Presiona T para obtener información de debug
        if (Input.GetKeyDown(testKey) && showDebugInfo) {
            ShowDebugInfo();
        }
    }

    void ShowDebugInfo() {
        if (swarm == null) {
            Debug.LogWarning("[FireflyDemo] No hay enjambre asignado.");
            return;
        }
        
        Debug.Log("=== FIREFLY SWARM DEBUG INFO ===");
        Debug.Log($"Enjambre activo: {swarm.enabled}");
        Debug.Log($"Posición del enjambre: {swarm.transform.position}");
        Debug.Log($"Número de luciérnagas: {swarm.transform.childCount}");
        
        // Información de distancia si hay objetivo
        GameObject target = GameObject.Find("Checkpoint_01");
        if (target != null) {
            float distance = Vector2.Distance(transform.position, target.transform.position);
            Debug.Log($"Distancia al objetivo: {distance:F2} unidades");
            
            if (distance < 3f) {
                Debug.Log("→ Muy cerca del objetivo (enjambre desactivado)");
            }
            else if (distance > 15f) {
                Debug.Log("→ Muy lejos del objetivo (enjambre desactivado)");
            }
            else {
                Debug.Log("→ En rango de activación (enjambre activo)");
            }
        }
        
        Debug.Log("================================");
    }

    void OnDrawGizmos() {
        if (!showDebugInfo) return;
        
        // Dibujar radio de detección alrededor del jugador
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 3f);
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 15f);
    }
}
