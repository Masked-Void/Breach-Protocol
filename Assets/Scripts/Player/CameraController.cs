using UnityEngine;

/*
 * Script: CameraController
 *
 * Description:
 * Mouse look. Sits on the camera, which is a child of the player. Vertical
 * rotation goes on the camera, horizontal goes on the player root so the body
 * turns with the view.
 *
 * Interacts With:
 * - GameManager (stops looking around while paused)
 *
 * Notes:
 * - Locks and hides the cursor on Start. Nothing unlocks it except the pause
 *   menu, so a build with no menu leaves the cursor trapped.
 */
public class CameraController : MonoBehaviour
{
    [Tooltip("mouse look speed, multiplied into the raw mouse delta")]
    [SerializeField] int sens;

    [Tooltip("how far down the player can look, negative degrees")]
    [SerializeField] int lockVertMin;

    [Tooltip("how far up the player can look, positive degrees")]
    [SerializeField] int lockVertMax;

    // accumulated vertical angle, kept separate so it can be clamped
    float camRotX;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (!GameManager.instance.isPaused)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * sens;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sens;
            camRotX -= mouseY;
            camRotX = Mathf.Clamp(camRotX, lockVertMin, lockVertMax);
            transform.localRotation = Quaternion.Euler(camRotX, 0, 0);
            transform.parent.Rotate(Vector3.up * mouseX);
        }
    }
}
