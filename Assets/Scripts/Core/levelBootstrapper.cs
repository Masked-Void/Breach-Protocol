using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelBootstrapper : MonoBehaviour
{
    [Header("Bootstrap")]
    [Tooltip("Scene name of the persistent scene that contains the managers")]
    [SerializeField] private string bootstrapSceneName = "Bootstrap";

    private void Awake()
    {
        if (SceneManager.GetSceneByName(bootstrapSceneName).isLoaded)
        {
            return;
        }

        SceneManager.LoadScene(bootstrapSceneName, LoadSceneMode.Additive);

    }
}
