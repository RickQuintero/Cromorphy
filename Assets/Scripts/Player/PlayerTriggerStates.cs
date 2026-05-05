using UnityEngine;

[RequireComponent(typeof(PlayerController2D))]
public class PlayerTriggerStates : MonoBehaviour
{
    private PlayerController2D _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Depredator"))
            _controller.EnterRagdoll(other.transform);
        else if (other.CompareTag("Spike"))
            _controller.EnterRagdoll();
    }
}
