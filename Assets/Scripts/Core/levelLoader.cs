using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


// Lives in bootstrp, swaps which level scene is loaded while the managers stay put
// Only one level scene should be loaded at a time, and the level scene should be the only scene that is unloaded and loaded
public class levelLoader : MonoBehaviour
{
    
    public static levelLoader instance;

    [Header("Scenes")]
    [Tooltip("Scene name of the persistent scene that contains the managers")]
    [SerializeField] private string bootstrapSceneName = "Bootstrap";

    [Tooltip("Level to load when not asked, leave blank to load nothing")]
    [SerializeField] private string fallbackLevelName = "";

    [Header("Spawn")]
    [Tooltip("Object name in the level scene to spawn the player")]
    [SerializeField] private string playerSpawnObjectName = "Player Spawn Pos";

    // set by the title screen before it loads bootstrap.
    private static string requestedLevelName = "";

    // scene name of the level scene that is currently loaded, or empty if no level scene is loaded
    private static string currentLevel = "";

    public string CurrentLevel => currentLevel;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        if (SceneManager.sceneCount > 1)
        {
            currentLevel = findOpenLevelName();
            placePlayerAtSpawn();
            return;
        }

        string wanted = string.IsNullOrEmpty(requestedLevelName) ? fallbackLevelName : requestedLevelName;

        if (string.IsNullOrEmpty(wanted))
        {
            return;
        }

        StartCoroutine(LoadLevel(wanted));
    }


    public IEnumerator LoadLevel(string levelName)
    {
        if (!string.IsNullOrEmpty(currentLevel))
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentLevel);
            while (!unload.isDone)
            {
                yield return null;
            }

            currentLevel = "";
        }

        AsyncOperation load = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
        while (!load.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(levelName));
        currentLevel = levelName;

        placePlayerAtSpawn();
    }


    private void placePlayerAtSpawn()
    {
        GameObject player = gameManager.instance != null ? gameManager.instance.player : null;

        if (player == null)
        {
            return;
        }

        GameObject spawnPoint = GameObject.Find(playerSpawnObjectName);

        if (spawnPoint == null)
        {
            Debug.LogWarning("levelLoader: no '"+playerSpawnObjectName+"' found in " + currentLevel,this);
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
        }

        player.transform.position = spawnPoint.transform.position;
        player.transform.rotation = spawnPoint.transform.rotation;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }


    private string findOpenLevelName()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != bootstrapSceneName)
            {
                return scene.name;
            }
        }

        return "";
    }
}
