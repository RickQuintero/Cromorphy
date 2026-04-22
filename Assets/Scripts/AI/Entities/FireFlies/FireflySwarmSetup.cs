using UnityEngine;

/// <summary>
/// Script de ayuda para configurar rápidamente el enjambre de luciérnagas
/// Adjunta este script a un GameObject vacío en tu escena
/// </summary>
public class FireflySwarmSetup : MonoBehaviour {
    
    [Header("Referencias Automáticas")]
    [Tooltip("Si está vacío, buscará automáticamente el objeto con tag 'Player'")]
    public Transform playerTransform;
    
    [Tooltip("El punto de destino hacia donde guiar (ej: el círculo amarillo)")]
    public Transform destinationTransform;
    
    [Header("Configuración Rápida")]
    [Tooltip("Distancia desde el jugador para activar el enjambre")]
    public float activationDistance = 15f;
    
    [Tooltip("Distancia al objetivo para desactivar el enjambre")]
    public float deactivationDistance = 3f;
    
    [Tooltip("Número de luciérnagas en el enjambre")]
    public int numberOfFireflies = 8;
    
    [Header("Mensajes Personalizados")]
    [Tooltip("Puedes cambiar estos mensajes. Se traducirán automáticamente a Morse")]
    public string rightMessage = "DERECHA";
    public string leftMessage = "IZQUIERDA";
    public string upMessage = "ARRIBA";
    public string downMessage = "ABAJO";
    
    private FireflySwarm swarm;

    void Start() {
        SetupSwarm();
    }

    /// <summary>
    /// Configura el enjambre automáticamente
    /// </summary>
    [ContextMenu("Setup Swarm")]
    public void SetupSwarm() {
        // Buscar o crear el componente FireflySwarm
        swarm = GetComponent<FireflySwarm>();
        if (swarm == null) {
            swarm = gameObject.AddComponent<FireflySwarm>();
        }
        
        // Buscar jugador si no está asignado
        if (playerTransform == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                playerTransform = player.transform;
                Debug.Log($"[FireflySwarm] Jugador encontrado automáticamente: {player.name}");
            }
            else {
                Debug.LogWarning("[FireflySwarm] No se encontró ningún objeto con tag 'Player'. Asigna manualmente el jugador.");
            }
        }
        
        // Verificar destino
        if (destinationTransform == null) {
            Debug.LogWarning("[FireflySwarm] No hay destino asignado. Asigna el transform del objetivo en el inspector.");
        }
        
        // Configurar el swarm
        swarm.SetPlayer(playerTransform);
        swarm.SetTarget(destinationTransform);
        
        Debug.Log($"[FireflySwarm] Configuración completada. Enjambre listo para usar.");
    }

    /// <summary>
    /// Cambia el destino del enjambre en tiempo de ejecución
    /// </summary>
    public void ChangeDestination(Transform newDestination) {
        destinationTransform = newDestination;
        if (swarm != null) {
            swarm.SetTarget(newDestination);
            Debug.Log($"[FireflySwarm] Nuevo destino establecido: {newDestination.name}");
        }
    }

    /// <summary>
    /// Activa o desactiva el sistema completo
    /// </summary>
    public void SetActive(bool active) {
        if (swarm != null) {
            swarm.enabled = active;
        }
    }

    void OnDrawGizmos() {
        // Dibujar línea hacia el destino para visualización
        if (destinationTransform != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destinationTransform.position, 0.5f);
            Gizmos.DrawIcon(destinationTransform.position, "sv_icon_dot0_pix16_gizmo", true);
        }
        
        if (playerTransform != null && destinationTransform != null) {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawLine(playerTransform.position, destinationTransform.position);
        }
    }
}
