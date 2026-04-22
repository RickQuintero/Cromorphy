using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gestiona un enjambre de luciérnagas que guían al jugador hacia un objetivo
/// </summary>
public class FireflySwarm : MonoBehaviour {
    
    [Header("Target Settings")]
    [SerializeField] private Transform targetDestination;
    [SerializeField] private Transform player;
    [SerializeField] private float activationDistance = 15f;
    [SerializeField] private float deactivationDistance = 3f;
    
    [Header("Swarm Settings")]
    [SerializeField] private GameObject fireflyPrefab;
    [SerializeField] private int fireflyCount = 8;
    [SerializeField] private float swarmRadius = 2f;
    [SerializeField] private float distanceFromPlayer = 3f;
    
    [Header("Morse Message")]
    [SerializeField] private string[] directionMessages = new string[] {
        "DERECHA", 
        "IZQUIERDA",
        "ARRIBA",
        "ABAJO"
    };
    
    [Header("Direction Settings")]
    [SerializeField] private float updateInterval = 2f;
    [SerializeField] private bool showDebugGizmos = true;
    
    private List<FireflyGuide> fireflies = new List<FireflyGuide>();
    private bool isActive = false;
    private float lastUpdateTime = 0f;
    private Vector2 currentDirection;

    void Start() {
        // Crear el enjambre
        CreateSwarm();
        
        // Buscar jugador si no está asignado
        if (player == null) {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) {
                player = playerObj.transform;
            }
        }
        
        // Desactivar inicialmente
        SetSwarmActive(false);
    }

    void Update() {
        if (player == null || targetDestination == null) return;
        
        float distanceToTarget = Vector2.Distance(player.position, targetDestination.position);
        
        // Activar/desactivar según distancia
        if (!isActive && distanceToTarget > deactivationDistance && distanceToTarget < activationDistance) {
            SetSwarmActive(true);
        }
        else if (isActive && (distanceToTarget <= deactivationDistance || distanceToTarget > activationDistance)) {
            SetSwarmActive(false);
        }
        
        // Actualizar posición y dirección del enjambre
        if (isActive) {
            UpdateSwarmPosition();
            
            if (Time.time - lastUpdateTime > updateInterval) {
                UpdateDirection();
                lastUpdateTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Crea el enjambre de luciérnagas
    /// </summary>
    private void CreateSwarm() {
        for (int i = 0; i < fireflyCount; i++) {
            GameObject fireflyObj;
            
            if (fireflyPrefab != null) {
                fireflyObj = Instantiate(fireflyPrefab, transform);
            }
            else {
                // Crear luciérnaga básica si no hay prefab
                fireflyObj = new GameObject($"Firefly_{i}");
                fireflyObj.transform.parent = transform;
                
                // Añadir SpriteRenderer
                SpriteRenderer sr = fireflyObj.AddComponent<SpriteRenderer>();
                sr.color = new Color(1f, 0.9f, 0.3f, 1f);
                
                // Crear sprite circular simple
                Texture2D tex = new Texture2D(32, 32);
                for (int x = 0; x < 32; x++) {
                    for (int y = 0; y < 32; y++) {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                        float alpha = Mathf.Clamp01(1f - (dist / 16f));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
                
                // Añadir luz 2D
                var light = fireflyObj.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = new Color(1f, 0.9f, 0.3f, 1f);
                light.intensity = 0.5f;
                light.pointLightOuterRadius = 2f;
            }
            
            // Añadir componente FireflyGuide si no lo tiene
            FireflyGuide guide = fireflyObj.GetComponent<FireflyGuide>();
            if (guide == null) {
                guide = fireflyObj.AddComponent<FireflyGuide>();
            }
            
            // Posicionar en círculo
            float angle = (i / (float)fireflyCount) * Mathf.PI * 2f;
            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * swarmRadius,
                Mathf.Sin(angle) * swarmRadius,
                0f
            );
            fireflyObj.transform.localPosition = pos;
            
            fireflies.Add(guide);
        }
    }

    /// <summary>
    /// Actualiza la posición del enjambre
    /// </summary>
    private void UpdateSwarmPosition() {
        if (player == null) return;
        
        // Calcular dirección hacia el objetivo
        currentDirection = ((Vector2)targetDestination.position - (Vector2)player.position).normalized;
        
        // Posicionar el enjambre delante del jugador en dirección al objetivo
        Vector3 targetPos = player.position + (Vector3)currentDirection * distanceFromPlayer;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
    }

    /// <summary>
    /// Actualiza la dirección y mensaje Morse
    /// </summary>
    private void UpdateDirection() {
        if (player == null || targetDestination == null) return;
        
        // Calcular dirección
        Vector2 direction = ((Vector2)targetDestination.position - (Vector2)player.position).normalized;
        
        // Determinar mensaje según dirección predominante
        string message = GetDirectionMessage(direction);
        
        // Asignar mensaje a todas las luciérnagas
        foreach (var firefly in fireflies) {
            if (firefly != null) {
                firefly.SetMorseMessage(message);
            }
        }
    }

    /// <summary>
    /// Obtiene el mensaje de dirección según el vector
    /// </summary>
    private string GetDirectionMessage(Vector2 direction) {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Normalizar ángulo a 0-360
        if (angle < 0) angle += 360f;
        
        // Determinar dirección predominante
        if (angle >= 45f && angle < 135f) {
            return directionMessages[2]; // ARRIBA (índice 2)
        }
        else if (angle >= 135f && angle < 225f) {
            return directionMessages[1]; // IZQUIERDA (índice 1)
        }
        else if (angle >= 225f && angle < 315f) {
            return directionMessages[3]; // ABAJO (índice 3)
        }
        else {
            return directionMessages[0]; // DERECHA (índice 0)
        }
    }

    /// <summary>
    /// Activa o desactiva el enjambre
    /// </summary>
    private void SetSwarmActive(bool active) {
        isActive = active;
        
        // Primero activar/desactivar todos los GameObjects
        foreach (var firefly in fireflies) {
            if (firefly != null) {
                firefly.gameObject.SetActive(active);
            }
        }
        
        // Luego actualizar dirección o detener parpadeo
        if (active) {
            UpdateDirection();
        }
        else {
            foreach (var firefly in fireflies) {
                if (firefly != null) {
                    firefly.StopBlinking();
                }
            }
        }
    }

    /// <summary>
    /// Establece el objetivo del enjambre
    /// </summary>
    public void SetTarget(Transform target) {
        targetDestination = target;
    }

    /// <summary>
    /// Establece el jugador
    /// </summary>
    public void SetPlayer(Transform playerTransform) {
        player = playerTransform;
    }

    void OnDrawGizmos() {
        if (!showDebugGizmos) return;
        
        // Dibujar radio de activación
        if (targetDestination != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetDestination.position, activationDistance);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetDestination.position, deactivationDistance);
        }
        
        // Dibujar línea hacia objetivo
        if (player != null && targetDestination != null && isActive) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(player.position, targetDestination.position);
        }
    }
}
