using System.Collections;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  POOLED PARTICLE  —  Auto-return component
//
//  Mirrors EntityPoolMember but for particles.
//  Added automatically by ParticleManager to every pooled particle.
//
//  Lifecycle:
//    ParticleManager.PlayEffect_FX()
//      → pulls from pool queue
//      → calls PooledParticle.Play(position, rotation, parent)
//        → moves, parents, activates, starts ParticleSystem
//        → StartCoroutine(WaitAndReturn)
//          → waits until ParticleSystem.isPlaying == false
//          → calls ParticleManager.ReturnToPool(this)
//            → SetActive(false), reparent to pool container, enqueue
//
//  No Update() loop — uses a coroutine that sleeps between checks.
//  No memory leaks — every Play() is matched by exactly one ReturnToPool().
// ─────────────────────────────────────────────────────────────────────────────
[RequireComponent(typeof(ParticleSystem))]
public class PooledParticle : MonoBehaviour
{
    // Set by ParticleManager after Instantiate — never changes
    public string EffectName { get; private set; }

    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();

        // Ensure particles don't loop — looping systems never auto-return
        if (_ps != null)
        {
            var main = _ps.main;
            if (main.loop)
            {
                Debug.LogWarning($"[PooledParticle] '{name}' has looping ParticleSystem. " +
                                 "Looping effects never auto-return to pool. " +
                                 "Disable loop or call ReturnToPool() manually.");
            }
        }
    }

    /// <summary>Called by ParticleManager after pulling from pool.</summary>
    public void Initialise(string effectName)
    {
        EffectName = effectName;
    }

    /// <summary>
    /// Position, parent, play, and schedule auto-return.
    /// Parent is optional — pass null to leave at world root.
    /// </summary>
    public void Play(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        transform.SetPositionAndRotation(position, rotation);
        transform.SetParent(parent, worldPositionStays: true);

        gameObject.SetActive(true);

        if (_ps != null)
        {
            _ps.Clear();
            _ps.Play();
        }

        StartCoroutine(WaitAndReturn());
    }

    /// <summary>
    /// Manually return to pool early (e.g. for looping effects or on scene change).
    /// </summary>
    public void ReturnToPool()
    {
        StopAllCoroutines();
        if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleManager.Instance?.ReturnToPool(this);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private IEnumerator WaitAndReturn()
    {
        if (_ps == null) yield break;

        // Wait one frame so isPlaying becomes true
        yield return null;

        // Poll until the particle system finishes — checks every 0.1s (no per-frame overhead)
        while (_ps.isPlaying)
            yield return new WaitForSeconds(0.1f);

        ParticleManager.Instance?.ReturnToPool(this);
    }
}
