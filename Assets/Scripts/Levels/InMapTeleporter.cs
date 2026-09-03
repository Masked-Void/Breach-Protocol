using UnityEngine;

using UnityEngine;

// moves the player to another spot in the same level when they walk into it.
// used for shortcuts and vertical links, unlike BossTeleporter which swaps scenes.
public class InMapTeleporter : MonoBehaviour
{
    [Tooltip("where the player ends up, an empty object placed in the level")]
    [SerializeField] Transform teleportPos;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && teleportPos !=null) {
            GameManager.instance.player.transform.position = teleportPos.position;
        }
    }
}
