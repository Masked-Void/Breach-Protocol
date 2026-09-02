using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    [Header("Time Scale Range")]
    [SerializeField] private float minTimeScale = 0.05f;
    [SerializeField] private float maxTimeScale = 1f;
    [SerializeField] private float moveMaxTimeScale = 0.85f;

    [Header("Movement Influence")]
    [Tooltip("Higher values require more movement before time speeds up strongly.")]
    [SerializeField] private float movementCurvePower = 1.35f;

    [Header("Heartbeat Influence")]
    [Range(0f, 1f)]
    [SerializeField] private float bpmInfluence = 0.40f;

    [Header("Smoothing")]
    [SerializeField] private float timeScaleSmoothing = 10f;

    [Header("Runtime Override")]
    [SerializeField] private bool hasTimeScaleOverride;
    [SerializeField] private float overrideTimeScale;

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

        // Preserve the project's normal physics step.
        baseFixedDeltaTime = Time.fixedDeltaTime / Mathf.Max(Time.timeScale, 0.0001f);

        currentTimeScale = Mathf.Clamp(minTimeScale, 0.001f, maxTimeScale);
        ApplyTimeScale(currentTimeScale);

    }

    private void Update()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;

        float targetTimeScale;

        if (hasTimeScaleOverride)
        {
            // Scorestreaks such as Adrenaline temporarily take full control
            // over world speed. Movement and heartbeat cannot fight it.
            targetTimeScale = overrideTimeScale;
        }
        else
        {
            if (GameManager.instance == null || GameManager.instance.playerScript == null)
                return;

            float movement01 = Mathf.Clamp01(
                GameManager.instance.playerScript.SpeedPercent
            );

            movement01 = Mathf.Pow(movement01, movementCurvePower);

            float movementScale = Mathf.Lerp(
                minTimeScale,
                moveMaxTimeScale,
                movement01
            );

            float stress01 = HeartbeatManager.instance != null
                ? HeartbeatManager.instance.StressPercent
                : 0f;

            float heartbeatInfluence = Mathf.Clamp01(stress01 * bpmInfluence);

            targetTimeScale = Mathf.Lerp(
                movementScale,
                maxTimeScale,
                heartbeatInfluence
            );
        }

        // Frame-rate-independent exponential smoothing.
        float blend = 1f - Mathf.Exp(-timeScaleSmoothing * Time.unscaledDeltaTime);

        currentTimeScale = Mathf.Lerp(
            currentTimeScale,
            targetTimeScale,
            blend
        );

        

        ApplyTimeScale(currentTimeScale);
    }

    private void ApplyTimeScale(float newTimeScale)
    {
        newTimeScale = Mathf.Clamp(newTimeScale, minTimeScale, maxTimeScale);

        if (Mathf.Abs(Time.timeScale - newTimeScale) < 0.0001f)
            return;

        Time.timeScale = newTimeScale;
        Time.fixedDeltaTime = baseFixedDeltaTime * newTimeScale;
    }

    // Sets the normal world speed immediately. Persistent effects should
    // use setTimeScaleOverride instead so Update() does not overwrite them.
    public void SetTimeScale(float newTimeScale)
    {
        if (GameManager.instance != null && GameManager.instance.isPaused)
            return;

        currentTimeScale = Mathf.Clamp(newTimeScale, minTimeScale, maxTimeScale);
        ApplyTimeScale(currentTimeScale);
    }

    // Used by Adrenaline and future scorestreaks.

    public void SetTimeScaleOverride(float newTimeScale)
    {
        overrideTimeScale = Mathf.Clamp(newTimeScale, minTimeScale, maxTimeScale);
        hasTimeScaleOverride = true;

        // Adrenaline should feel immediate rather than taking several frames
        // Apply it instantly on activation.
        currentTimeScale = overrideTimeScale;

        if (GameManager.instance == null || !GameManager.instance.isPaused)
            ApplyTimeScale(currentTimeScale);
    }

    public void ClearTimeScaleOverride()
    {
        hasTimeScaleOverride = false;

        // Do not snap back here. The regular Update formula smoothly returns
        // from the override to movement + heartbeat controlled time.
    }

    public bool ActiveTimeScaleOverride => hasTimeScaleOverride;

    public float TimeScale => currentTimeScale;

    public void PauseTime()
    {
        if (Time.timeScale > 0f)
            currentTimeScale = Time.timeScale;

        Time.timeScale = 0f;
    }

    public void UnpauseTime()
    {
        ApplyTimeScale(currentTimeScale);
    }

    private void OnValidate()
    {
        minTimeScale = Mathf.Max(0.001f, minTimeScale);
        maxTimeScale = Mathf.Max(minTimeScale, maxTimeScale);
        moveMaxTimeScale = Mathf.Clamp(moveMaxTimeScale, minTimeScale, maxTimeScale);
        movementCurvePower = Mathf.Max(0.01f, movementCurvePower);
        timeScaleSmoothing = Mathf.Max(0f, timeScaleSmoothing);
        bpmInfluence = Mathf.Clamp01(bpmInfluence);

        if (hasTimeScaleOverride)
            overrideTimeScale = Mathf.Clamp(overrideTimeScale, minTimeScale, maxTimeScale);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}