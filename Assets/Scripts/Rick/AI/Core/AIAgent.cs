using UnityEngine;

/// <summary>
/// Abstract base component for all AI entities in the ecosystem.
///
/// Responsibilities:
///   - Owns and ticks the AIStateMachine each Update/FixedUpdate
///   - Exposes FaceDirection and Target as shared runtime data
///   - Provides StopHorizontal and Despawn utilities
///   - Hooks pool re-activation: OnEnable → OnSpawned()
///
/// Subclasses override:
///   RegisterStates()  → add entity-specific AIState instances (called once in Awake)
///   OnSpawned()       → reset runtime data, enter the opening FSM state (called per spawn)
///
/// Locomotion note:
///   Simple agents (Mouse) set Rb.linearVelocity directly from their states.
///   Complex agents (Snake) delegate locomotion to CreatureController2D and
///   only set direction flags — they still require Rigidbody2D through that component.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public abstract class AIAgent : MonoBehaviour
{
    [Header("Shared Physics")]
    public float moveSpeed = 3f;

    [Header("Shared Detection")]
    public float attackRange = 0.4f;
    public LayerMask solidLayer;

    // ── Shared runtime ────────────────────────────────────────────────────

    /// <summary>Movement / facing direction. +1 = right, -1 = left.</summary>
    [System.NonSerialized] public float FaceDirection = 1f;

    /// <summary>Active hunt or threat target. Null when the agent has no focus.</summary>
    [System.NonSerialized] public Transform Target;

    // ── Component refs ────────────────────────────────────────────────────

    public Rigidbody2D      Rb         { get; private set; }
    public EntityPoolMember PoolMember { get; private set; }

    // ── State machine ─────────────────────────────────────────────────────

    public AIStateMachine StateMachine { get; private set; }

    // ─────────────────────────────────────────────────────────────────────

    protected virtual void Awake()
    {
        Rb         = GetComponent<Rigidbody2D>();
        PoolMember = GetComponent<EntityPoolMember>();

        StateMachine = new AIStateMachine(this);
        RegisterStates();   // concrete subclass populates the machine
    }

    /// <summary>
    /// Called by Unity each time the pool re-activates this object (SetActive true).
    /// Dispatches to OnSpawned so subclasses reset their own state.
    /// </summary>
    protected virtual void OnEnable() => OnSpawned();

    protected virtual void Update()      => StateMachine.Tick();
    protected virtual void FixedUpdate() => StateMachine.FixedTick();

    // ── Abstract contract ─────────────────────────────────────────────────

    /// <summary>
    /// Register all AIState instances this entity needs.
    /// Called once during Awake — states are reused across every spawn cycle.
    /// </summary>
    protected abstract void RegisterStates();

    /// <summary>
    /// Reset all runtime data and push the opening FSM state.
    /// Called once per spawn cycle from OnEnable.
    /// </summary>
    protected abstract void OnSpawned();

    // ── Shared utilities ──────────────────────────────────────────────────

    /// <summary>
    /// Set horizontal velocity to zero without touching the vertical component.
    /// Safe to call while airborne (preserves gravity/jump arc).
    /// </summary>
    public void StopHorizontal() =>
        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);

    /// <summary>
    /// Return this entity to its pool. Clears Target first so stale references
    /// cannot bleed into the next spawn cycle.
    /// </summary>
    public void Despawn()
    {
        Target = null;
        PoolMember?.ReturnToPool();
    }
}
