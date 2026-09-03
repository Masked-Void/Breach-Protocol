using System.Collections;
using UnityEngine;

/*
 * Script: KillstreakBase
 *
 * Description:
 * Abstract base for the ten scorestreaks. Each subclass implements its own
 * effect; the manager rolls one and activates it.
 *
 * Duration decides the shape of the streak:
 *   > 0  timed, ends itself after that many real seconds
 *   = 0  instant, fires once and ends immediately
 *   < 0  manual, stays active until something calls Deactivate
 *
 * Interacts With:
 * - KillstreakManager (rolls, activates, and is told when one ends)
 * - EnemyBase (several streaks mutate or kill enemies)
 *
 * Notes:
 * - Timer runs on unscaled time and skips while paused, so a streak can't be
 *   burned through by slowing the world down.
 * - Audit finding: six of the ten streaks set flags nothing consumes.
 */
public abstract class KillstreakBase : MonoBehaviour
{
    [Header("Scorestreak Info")]
    [Tooltip("shown in the stored streak slot on the hud")]
    [SerializeField] protected string killstreakName = "Unnamed Program";

    [Tooltip("Seconds in real time. > 0 timed, 0 instant, < 0 stays active until Deactivate() is called.")]
    [SerializeField] protected float duration = 0f;

    public bool isActive { get; private set; }

    private Coroutine runRoutine;

    public string KillstreakName()
    {
        return string.IsNullOrWhiteSpace(killstreakName)
            ? gameObject.name
            : killstreakName;
    }

    public float Duration => duration;

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

    // ends the streak, tells the manager, and lets the subclass clean up.
    // guarded so a double Deactivate can't fire onDeactivate twice.
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

    // subclasses implement these. onTick is optional and only fires on timed streaks.
    protected abstract void onActivate();

    protected virtual void onTick(float unscaledDeltaTime) { }

    protected abstract void onDeactivate();
}
