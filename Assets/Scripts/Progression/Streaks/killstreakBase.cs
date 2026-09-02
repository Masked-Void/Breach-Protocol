using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for player-earned scorestreak programs.
/// duration > 0 = timed, duration == 0 = instant, duration < 0 = manual end.
/// </summary>
public abstract class killstreakBase : MonoBehaviour
{
    [Header("Scorestreak Info")]
    [SerializeField] protected string killstreakName = "Unnamed Program";

    [Tooltip("Seconds in real time. > 0 timed, 0 instant, < 0 stays active until Deactivate() is called.")]
    [SerializeField] protected float duration = 0f;

    public bool isActive { get; private set; }

    private Coroutine runRoutine;

    public string GetKillstreakName()
    {
        return string.IsNullOrWhiteSpace(killstreakName)
            ? gameObject.name
            : killstreakName;
    }

    public float GetDuration()
    {
        return duration;
    }

    public void Activate()
    {
        if (isActive)
            return;

        isActive = true;
        onActivate();

        // A subclass may fail setup and call Deactivate() from onActivate().
        if (!isActive)
            return;

        if (duration == 0f)
        {
            endStreak();
        }
        else if (duration > 0f)
        {
            runRoutine = StartCoroutine(runTimer());
        }
        // duration < 0 is a manual/charge-based streak and remains active.
    }

    public void Deactivate()
    {
        if (!isActive)
            return;

        if (runRoutine != null)
        {
            StopCoroutine(runRoutine);
            runRoutine = null;
        }

        endStreak();
    }

    private IEnumerator runTimer()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return null;

            if (gameManager.instance != null && gameManager.instance.isPaused)
                continue;

            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            onTick(dt);
        }

        runRoutine = null;
        endStreak();
    }

    private void endStreak()
    {
        if (!isActive)
            return;

        isActive = false;
        onDeactivate();

        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.streakEnded(this);
        }
    }

    protected abstract void onActivate();

    protected virtual void onTick(float unscaledDeltaTime) { }

    protected abstract void onDeactivate();
}
