using UnityEngine;

/// <summary>
/// Attached to the Player. Single entry point for external systems
/// (predators, hazards) to trigger a death or dying sequence.
///
/// On TriggerDying():
///   1. Disables PlayerController2D so the player loses all input
///   2. Notifies GameManager to start the predator-death coroutine
///
/// The player's Rigidbody2D is left active so the snake can physically
/// carry the player to the nest (it's now a child of the snake head).
/// </summary>
[RequireComponent(typeof(PlayerController2D))]
public class PlayerTriggerStates : MonoBehaviour
{
    private PlayerController2D _controller;

    private void Awake()
    {
        _controller = GetComponent<PlayerController2D>();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Called by a predator immediately after parenting the player to its head.
    /// </summary>
    /// <param name="attackOrigin">The predator's head transform (informational).</param>
    public void TriggerDying(Transform attackOrigin)
    {
        // Strip player control
        if (_controller != null)
            _controller.enabled = false;

        // Start the 6-second game-over sequence in GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.TriggerDyingByPredator();
        else
            Debug.LogWarning("[PlayerTriggerStates] GameManager.Instance is null.");
    }
}
