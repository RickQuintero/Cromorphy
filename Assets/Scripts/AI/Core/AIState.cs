/// <summary>
/// Abstract base class for every AI state.
///
/// Design principle: states are stateless objects.
/// All mutable runtime data lives on the AIAgent, not inside the state.
/// This means state instances can be registered once in Awake and reused
/// across every spawn cycle without allocation.
///
///   Enter     — one-shot setup when the machine transitions in
///   Tick      — per-frame logic (timers, animation triggers)
///   FixedTick — physics-driven logic (movement, raycasting)
///   Exit      — cleanup when the machine transitions out
/// </summary>
public abstract class AIState
{
    public abstract AIStateID ID { get; }

    /// <summary>Called once when entering this state.</summary>
    public virtual void Enter(AIAgent agent) { }

    /// <summary>Called every Update frame while in this state.</summary>
    public virtual void Tick(AIAgent agent) { }

    /// <summary>Called every FixedUpdate while in this state. Prefer for physics.</summary>
    public virtual void FixedTick(AIAgent agent) { }

    /// <summary>Called once when leaving this state.</summary>
    public virtual void Exit(AIAgent agent) { }
}
