using JetBrains.Annotations;
using UnityEngine;

// connects the tutorial to the Bootstrap player's controller. PlayerController has no lock or teleport of its own,
// so this disables it to lock, and teleports the way LevelLoader does
public class MovementAdapter : MonoBehaviour, ITutorialMovement
{
    [Header("Input")]
    [Tooltip("the axis PlayerController reads for strafing")]
    [SerializeField] private string horizontalAxis = "Horizontal";

    [Tooltip("the axis PlayerController reads for forward and back")]
    [SerializeField] private string verticalAxis = "Vertical";

    private bool isLocked;

    // raw input, the same input that adds movement stress, so pushing into a wall still speeds time up
    public Vector2 MoveInput => isLocked ? Vector2.zero:new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis));

    private void OnDestroy()
    {
        if (isLocked)
        {
            SetMovementLocked(false);
        }
    }

    public void SetMovementLocked(bool shouldLock)
    {
        isLocked = shouldLock;

        PlayerController controller = GameManager.instance != null ? GameManager.instance.playerScript : null;

        if (controller == null)
        {
            return;
        }

        controller.enabled = !shouldLock;

        if (shouldLock && HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.NotifyMoving(false);
        }
    }

    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        GameObject player = GameManager.instance != null ? GameManager.instance.player : null;

        if (player == null)
        {
            return;
        }

        CharacterController body = player.GetComponent<CharacterController>();

        if (body != null)
        {
            body.enabled = false;
        }

        player.transform.SetPositionAndRotation(position, rotation);

        if (body != null)
        {
            body.enabled = true;
        }
    }


}
