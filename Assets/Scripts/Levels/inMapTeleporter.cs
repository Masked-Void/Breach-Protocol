using UnityEngine;

public class inMapTeleporter : MonoBehaviour
{
    [SerializeField] Transform teleportPos;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && teleportPos !=null) {
            gameManager.instance.player.transform.position = teleportPos.position;
        }
    }
}
