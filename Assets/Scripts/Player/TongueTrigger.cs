using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TongueTrigger : MonoBehaviour
{
    [Tooltip("The tongue attack system on the player character.")]
    public TongueSystemAttack tongueSystem;

    private GameObject  _capturedPrey;
    private Rigidbody2D _preyRb;

    void Update()
    {
        if (_capturedPrey == null || tongueSystem == null) return;
        if (!tongueSystem.HasRetracted) return;

        tongueSystem.ClearRetractedFlag();
        ReleasePrey();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_capturedPrey != null) return;

        Ent_Fly fly = other.GetComponent<Ent_Fly>();
        if (fly == null) return;

        _capturedPrey = other.gameObject;
        _preyRb       = _capturedPrey.GetComponent<Rigidbody2D>();

        if (_preyRb != null) _preyRb.isKinematic = true;
        _capturedPrey.transform.SetParent(transform);
    }

    private void ReleasePrey()
    {
        _capturedPrey.transform.SetParent(null);

        Ent_Fly fly = _capturedPrey.GetComponent<Ent_Fly>();
        if (fly != null) fly.ResetForPool();

        EntityPoolMember member = _capturedPrey.GetComponent<EntityPoolMember>();
        if (member != null)
            member.ReturnToPool();
        else
            _capturedPrey.SetActive(false);

        HungryPointManager.Instance.AddPoint();

        _capturedPrey = null;
        _preyRb       = null;
    }
}
