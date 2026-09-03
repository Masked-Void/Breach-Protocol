using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * Script: BossTeleporter
 *
 * Description:
 * The portal that opens once the player clears enough waves. Stays hidden
 * until then, and walking into it swaps the level for the boss arena.
 *
 * Interacts With:
 * - WaveManager (watches the wave count)
 * - LevelLoader (swaps the level while Bootstrap stays loaded)
 */
public class BossTeleporter : MonoBehaviour
{
    [Header("Appear Conditions")]
    [Tooltip("wave the portal appears on, gdd says 10 rounds then the boss")]
    public int appearOnWave = 10;

    [Header("Scene to load")]
    [Tooltip("Name of the boss scene. (Must be added and appear in build settings)")]
    public string bossSceneName = "BossFightArena";

    [Header("Misc")]
    [Tooltip("the visible portal, hidden on start and shown once the wave count is reached")]
    public GameObject portal;

    // true once the portal has appeared, stops Update checking every frame after
    private bool isOn = false;

    // lets the portal be forced open from the inspector for testing
    [ContextMenu("Turn on")]
    public void TurnOn()
    {
        isOn = true;
        if (portal != null) {
            portal.SetActive(true);
        }
    }

    private void Start() {
        if (portal != null) {
            portal.SetActive(false);
        } else {
            Debug.LogError("portal doesnt exist");
        }
    }

    private void Update() {

        if (isOn) {
            return;
        }

        if (WaveManager.instance == null) {
            return;
        }

        if (WaveManager.instance.CurrentWave >= appearOnWave) {
            isOn = true;
            if (portal != null) {
                portal.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // level loader swaps the level out from under bootstrap, so the
        // managers survive the trip to the boss arena
        if (LevelLoader.instance != null)
        {
            StartCoroutine(LevelLoader.instance.LoadLevel(bossSceneName));
        }
    }

}
