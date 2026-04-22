using UnityEngine;

public class WinnerZone : MonoBehaviour
{
    public string playerTag = "Player";
    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag(playerTag))
        {
            GameManager.Instance.TriggerWinner();
        }
    }
}
