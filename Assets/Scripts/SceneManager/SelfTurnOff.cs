using UnityEngine;

public class SelfTurnOff : MonoBehaviour
{
    //Deactiva this GameObject after a delay, used for things like the mouse's "poof" effect when it flees.
    public float delay = 10f;
    void Start()
    {
        Invoke("TurnOff", delay);
    }

    private void TurnOff()
    {
        gameObject.SetActive(false);
    }
}
