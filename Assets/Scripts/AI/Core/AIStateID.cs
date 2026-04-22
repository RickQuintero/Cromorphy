/// <summary>
/// Shared state identifiers for all AI agents in the ecosystem.
///
/// Design: not every agent uses every state — this is a shared vocabulary,
/// not a requirement. Extending the enum here is the only change needed
/// to introduce a new behavior type across the whole system.
///
/// Flee extends the core six states required by prey entities.
/// </summary>
public enum AIStateID
{
    Idle,
    Wander,
    Search,
    Chase,
    Attack,
    ReturnToNest,
    Flee    // Extension — prey agents running from predators
}
