using UnityEngine;

// tracks movement input and how long the player has gone without moving, on real time. the hint system reads it
public class PlayerActivityTracker : MonoBehaviour
{
    [Header("Links")]
    [Tooltip("The adapters on this same object, input comes from the movement adapter")]
    [SerializeField] private TutorialServices services;

    [Header("Input")]
    [Tooltip("input below this counts as not moving")]
    [Range(0f, 0.5f)][SerializeField] private float inputDeadzone = 0.1f;

    private float idleSeconds;
    private bool isMoving;
    public float IdleSeconds => idleSeconds;
    public bool IsMoving => isMoving;

    private void Update()
    {
        if(GameManager.instance!= null && GameManager.instance.isPaused)
        {
            return;
        }

        Vector2 input = services != null && services.Movement != null ? services.Movement.MoveInput : Vector2.zero;
        isMoving = input.sqrMagnitude > inputDeadzone * inputDeadzone;

        if (isMoving)
        {
            idleSeconds = 0f;
        }
        else
        {
            idleSeconds += Time.unscaledDeltaTime;
        }

    }

    public void ResetIdle()
    {
        idleSeconds = 0f;
    }
}
