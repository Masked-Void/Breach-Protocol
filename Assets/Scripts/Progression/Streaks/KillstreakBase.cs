using System.Collections;
using UnityEngine;

/*
 * Script: KillstreakBase
 *
 * Description:
 * Abstract base for the ten scorestreaks. Each subclass implements its own
 * effect and duration; the manager rolls one and activates it.
 *
 * Responsibilities:
 * - Common activate and deactivate lifecycle
 * - Report back to KillstreakManager when the streak ends
 *
 * Interacts With:
 * - KillstreakManager (rolls, activates, and is told when one ends)
 * - EnemyBase (several streaks mutate or kill enemies)
 *
 * Notes:
 * - Audit finding: six of the ten streaks set flags that nothing consumes.
 *   Setting a flag is not the same as having an effect.
 */


/// <summary>
/// Base class for player-earned scorestreak programs.
/// duration > 0 = timed, duration == 0 = instant, duration < 0 = manual end.
/// </summary>
public abstract class KillstreakBase : MonoBehaviour
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

            if (GameManager.instance != null && GameManager.instance.isPaused)
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

        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.StreakEnded(this);
        }
    }

    protected abstract void onActivate();

    protected virtual void onTick(float unscaledDeltaTime) { }

    protected abstract void onDeactivate();
}
