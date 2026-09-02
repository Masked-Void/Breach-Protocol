using UnityEngine;
using UnityEngine.SceneManagement;

public class BossTeleporter : MonoBehaviour
{

    [Header("Appear Conditions")]
    public int appearOnWave = 10;


    [Header("Scene to load")]
    [Tooltip("Name of the boss scene. (Must be added and appear in build settings)")]
    public string bossSceneName = "BossFightArena";

    [Header("Misc")]
    public GameObject portal;

    private bool isOn = false;


    [ContextMenu("Turn on")]
    public void turnOn() {
        Debug.Log("Turned On");
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

        if (WaveManager.instance.getCurrentWave() >= appearOnWave) {
            isOn = true;
            if (portal != null) {
                portal.SetActive(true);
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (!other.CompareTag("Player")) {
            return;
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene(bossSceneName);
    }

}
