using UnityEngine;

/// <summary>
/// Script de debug para diagnosticar problemas con el enjambre
/// </summary>
public class FireflySwarmDebug : MonoBehaviour {
    
    private float debugTimer = 0f;
    private bool debugPrinted = false;
    
    void Start() {
        Debug.Log("=== FIREFLY SWARM DEBUG - START ===");
        
        // Verificar componentes
        var setup = GetComponent<FireflySwarmSetup>();
        var swarm = GetComponent<FireflySwarm>();
        
        Debug.Log($"FireflySwarmSetup presente: {setup != null}");
        Debug.Log($"FireflySwarm presente: {swarm != null}");
        
        Debug.Log($"Número de hijos al inicio: {transform.childCount}");
    }
    
    void Update() {
        // Imprimir estado después de 2 segundos automáticamente
        debugTimer += Time.deltaTime;
        
        if (debugTimer > 2f && !debugPrinted) {
            debugPrinted = true;
            
            Debug.Log("=== ESTADO DESPUÉS DE 2 SEGUNDOS ===");
            Debug.Log($"Número de hijos: {transform.childCount}");
            
            // Verificar distancia
            var swarm = GetComponent<FireflySwarm>();
            if (swarm != null) {
                Debug.Log($"FireflySwarm está enabled: {swarm.enabled}");
                
                // Obtener referencias mediante reflexión
                var playerField = swarm.GetType().GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var targetField = swarm.GetType().GetField("targetDestination", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var activationField = swarm.GetType().GetField("activationDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var deactivationField = swarm.GetType().GetField("deactivationDistance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (playerField != null && targetField != null) {
                    var player = playerField.GetValue(swarm) as Transform;
                    var target = targetField.GetValue(swarm) as Transform;
                    
                    if (player != null && target != null) {
                        float distance = Vector2.Distance(player.position, target.position);
                        float activationDist = (float)activationField.GetValue(swarm);
                        float deactivationDist = (float)deactivationField.GetValue(swarm);
                        
                        Debug.Log($"Distancia actual: {distance:F2} unidades");
                        Debug.Log($"Rango de activación: {deactivationDist:F2} - {activationDist:F2}");
                        
                        if (distance < deactivationDist) {
                            Debug.LogWarning($"¡MUY CERCA! Distancia ({distance:F2}) < Deactivation ({deactivationDist})");
                        } else if (distance > activationDist) {
                            Debug.LogWarning($"¡MUY LEJOS! Distancia ({distance:F2}) > Activation ({activationDist})");
                        } else {
                            Debug.Log("✓ Distancia correcta - Las luciérnagas deberían estar activas");
                        }
                    }
                }
            }
            
            if (transform.childCount > 0) {
                for (int i = 0; i < transform.childCount; i++) {
                    var child = transform.GetChild(i);
                    Debug.Log($"Hijo {i}: {child.name} - Activo: {child.gameObject.activeSelf}");
                }
            } else {
                Debug.LogWarning("¡NO HAY HIJOS! Las luciérnagas no se crearon.");
            }
        }
    }
}
