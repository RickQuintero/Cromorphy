using System.Collections;
using UnityEngine;

public class FireflyMorseTrigger : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private string morseMessage = "";

    [Header("Trigger Behaviour")]
    [SerializeField] private bool  playOnce        = true;
    [SerializeField] private float triggerCooldown = 0f;
    [SerializeField] private bool  queueIfBusy     = false;

    private FireflyMorseController _morseController;
    private bool _onCooldown;
    private bool _pendingFire;

    private void Update()
    {
        if (!_pendingFire) return;
        if (_morseController == null || _morseController.IsPlaying) return;
        _pendingFire = false;
        Fire();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_onCooldown) return;

        if (_morseController == null)
            _morseController = FindObjectOfType<FireflyMorseController>();
        if (_morseController == null) return;

        if (_morseController.IsPlaying)
        {
            if (queueIfBusy) _pendingFire = true;
            return;
        }

        Fire();
    }

    private void Fire()
    {
        _morseController.PlayMorse(morseMessage);

        if (playOnce)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }
        else if (triggerCooldown > 0f)
        {
            _onCooldown = true;
            StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(triggerCooldown);
        _onCooldown = false;
    }
}
