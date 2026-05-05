using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic, allocation-free state machine.
/// Owned by an AIAgent — not a MonoBehaviour itself.
///
/// All states must be registered via RegisterState() before the machine
/// starts. SetState() calls Exit → Enter across the boundary.
/// Tick/FixedTick delegate to whichever state is currently active.
/// </summary>
public class AIStateMachine
{
    private readonly AIAgent _agent;
    private readonly Dictionary<AIStateID, AIState> _states = new();

    public AIState   CurrentState   { get; private set; }
    public AIStateID CurrentStateID => CurrentState?.ID ?? AIStateID.Idle;

    public AIStateMachine(AIAgent agent) => _agent = agent;

    // ── Registration ──────────────────────────────────────────────────────

    /// <summary>
    /// Register a state instance. Call once per state in AIAgent.RegisterStates().
    /// Overwrites any existing registration for the same ID.
    /// </summary>
    public void RegisterState(AIState state) => _states[state.ID] = state;

    // ── Transitions ───────────────────────────────────────────────────────

    /// <summary>
    /// Transition to a new state.
    /// No-ops if already in the requested state (prevents spurious Enter/Exit calls).
    /// Warns if the state was never registered.
    /// </summary>
    public void SetState(AIStateID id)
    {
        if (CurrentState?.ID == id) return;

        CurrentState?.Exit(_agent);

        if (_states.TryGetValue(id, out AIState next))
        {
            CurrentState = next;
            CurrentState.Enter(_agent);
        }
        else
        {
            Debug.LogWarning($"[AIStateMachine] State '{id}' is not registered on '{_agent.name}'.");
        }
    }

    // ── Tick passthrough ──────────────────────────────────────────────────

    public void Tick()      => CurrentState?.Tick(_agent);
    public void FixedTick() => CurrentState?.FixedTick(_agent);
}
