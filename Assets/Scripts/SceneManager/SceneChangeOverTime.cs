using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeOverTime : MonoBehaviour
{
    [SerializeField] private int targetSceneIndex = 1;
    [SerializeField] private float waitingTime = 5f;

    void Start()
    {
        StartCoroutine(LoadSceneAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay()
    {
        yield return new WaitForSeconds(waitingTime);
        SceneManager.LoadScene(targetSceneIndex);
    }
}
