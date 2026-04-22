using UnityEngine;

/// <summary>
/// Script simplificado para configurar el enjambre de luciérnagas
/// </summary>
public class FireflySwarmSetupSimple : MonoBehaviour {
    
    [Header("Referencias")]
    public Transform playerTransform;
    public Transform destinationTransform;
    
    [Header("Configuración")]
    public float activationDistance = 15f;
    public float deactivationDistance = 3f;
    public int numberOfFireflies = 8;
    
    [Header("Mensajes")]
    public string rightMessage = "DERECHA";
    public string leftMessage = "IZQUIERDA";
    public string upMessage = "ARRIBA";
    public string downMessage = "ABAJO";
    
    private FireflySwarm swarm;

    void Start() {
        SetupSwarm();
    }

    public void SetupSwarm() {
        swarm = GetComponent<FireflySwarm>();
        if (swarm == null) {
            swarm = gameObject.AddComponent<FireflySwarm>();
        }
        
        if (playerTransform == null) {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                playerTransform = player.transform;
            }
        }
        
        swarm.SetPlayer(playerTransform);
        swarm.SetTarget(destinationTransform);
    }
}
