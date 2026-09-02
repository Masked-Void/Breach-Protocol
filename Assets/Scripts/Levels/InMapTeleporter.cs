using UnityEngine;

public class InMapTeleporter : MonoBehaviour
{
    [SerializeField] Transform teleportPos;

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && teleportPos !=null) {
            GameManager.instance.player.transform.position = teleportPos.position;
        }
    }
}
