using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * Script: LevelBootstrapper
 *
 * Description:
 * Sits on one object in every level scene so you can press play from your own
 * level instead of always starting from Bootstrap. Does nothing during a normal
 * run because Bootstrap is already loaded by then.
 *
 * Responsibilities:
 * - Check whether Bootstrap is loaded when a level scene wakes up
 * - Load it additively underneath if it isn't
 *
 * Interacts With:
 * - Bootstrap.unity
 * - LevelLoader (which sees the level is already open and skips loading one)
 */

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
