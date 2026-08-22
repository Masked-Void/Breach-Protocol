using UnityEngine;

public class timeManager : MonoBehaviour
{
    public static timeManager instance;

    [Header("Time Scale Range")]
    [SerializeField] private float minTimeScale = 0.05f;
    [SerializeField] private float moveMaxTimeScale = 0.85f;
    [SerializeField] private float maxTimeScale = 1.0f;

    [Header("Movement")]
    [Tooltip("Higher values require more movement before time accelerates strongly.")]
    [SerializeField] private float movementCurvePower = 1.35f;

    [Header("Heartbeat Influence")]
    [Range(0f, 1f)]
    [Tooltip("How strongly maximum stress pushes the world toward maxTimeScale.")]
    [SerializeField] private float bpmInfluence = 0.40f;

    [Header("Smoothing")]
    [Tooltip("How quickly the world responds to time scale changes.")]
    [SerializeField] private float timeScaleSmoothing = 10f;


    private float currentTimeScale;
    private float baseFixedDeltaTime;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Recover the project's original physics step even if Time.timeScale
        // happened to already be modified.
        baseFixedDeltaTime =
            Time.fixedDeltaTime / Mathf.Max(Time.timeScale, 0.0001f);

        currentTimeScale = minTimeScale;

        ApplyTimeScale(currentTimeScale);

    }

    private void Update()
    {
        gameManager gm = gameManager.instance;

        if (gm == null ||
            gm.isPaused ||
            gm.playerScript == null)
        {
            return;
        }

        // PLAYER MOVEMENT

        float movement01 =
            Mathf.Clamp01(gm.playerScript.getSpeedPercent());

        // Gives us a tunable SUPERHOT-style response curve.
        movement01 = Mathf.Pow(
            movement01,
            movementCurvePower
        );

        float movementScale = Mathf.Lerp(
            minTimeScale,
            moveMaxTimeScale,
            movement01
        );

        // HEARTBEAT

        float stress01 = 0f;

        if (heartbeatManager.instance != null)
        {
            stress01 =
                heartbeatManager.instance.getStressPercent();
        }

        // bpmInfluence is now a percentage, not a raw
        // time-scale addition.
        float heartbeatInfluence =
            Mathf.Clamp01(stress01 * bpmInfluence);

        // Heartbeat pushes the movement-generated scale
        // toward maxTimeScale.
        float targetTimeScale = Mathf.Lerp(
            movementScale,
            maxTimeScale,
            heartbeatInfluence
        );

        // FRAME-RATE-INDEPENDENT SMOOTHING

        float blend =
            1f -
            Mathf.Exp(
                -timeScaleSmoothing *
                Time.unscaledDeltaTime
            );

        currentTimeScale = Mathf.Lerp(
            currentTimeScale,
            targetTimeScale,
            blend
        );

        

        ApplyTimeScale(currentTimeScale);
    }

    private void ApplyTimeScale(float newTimeScale)
    {
        newTimeScale = Mathf.Clamp(
            newTimeScale,
            minTimeScale,
            maxTimeScale
        );

        // Avoid unnecessary writes for extremely tiny changes.
        if (Mathf.Abs(Time.timeScale - newTimeScale) < 0.0001f)
            return;

        Time.timeScale = newTimeScale;

        // Keeps physics reasonably smooth during slow motion.
        Time.fixedDeltaTime =
            baseFixedDeltaTime * newTimeScale;
    }

    public void setTimeScale(float newTimeScale)
    {
        if (gameManager.instance != null &&
            gameManager.instance.isPaused)
        {
            return;
        }

        currentTimeScale = Mathf.Clamp(
            newTimeScale,
            minTimeScale,
            maxTimeScale
        );

        ApplyTimeScale(currentTimeScale);
    }

    public float getTimeScale()
    {
        return currentTimeScale;
    }

    public void pauseTime()
    {
        if (Time.timeScale > 0f)
        {
            currentTimeScale = Time.timeScale;
        }

        Time.timeScale = 0f;
    }

    public void unpauseTime()
    {
        ApplyTimeScale(currentTimeScale);
    }

    private void OnValidate()
    {
        minTimeScale = Mathf.Max(0.001f, minTimeScale);

        maxTimeScale = Mathf.Max(
            minTimeScale,
            maxTimeScale
        );

        moveMaxTimeScale = Mathf.Clamp(
            moveMaxTimeScale,
            minTimeScale,
            maxTimeScale
        );

        movementCurvePower =
            Mathf.Max(0.01f, movementCurvePower);

        timeScaleSmoothing =
            Mathf.Max(0f, timeScaleSmoothing);

        bpmInfluence =
            Mathf.Clamp01(bpmInfluence);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
