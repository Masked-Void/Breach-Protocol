using UnityEngine;
using System.Collections;

// the starting cell. the wall itself is the lesson, this only holds the player still for a moment so the slow crawl is seen first
public class TimeCellLesson : TutorialLesson
{
    [Header("Spawn Lock")]
    [Tooltip("real seconds the player can look but not move, so they see the wall crawl before learning to speed it up")]
    [SerializeField] private float spawnLockSeconds = 2f;

    private Coroutine lockRoutine;

    public override TutorialStage Stage => TutorialStage.TimeCell;

    public override void BeginLesson()
    {
        base.BeginLesson();
        stopLock();
        lockRoutine = StartCoroutine(LockMovementBriefly());

    }

    public override void EndLesson()
    {
        base.EndLesson();

        stopLock();
        setLocked(false);
    }

    private IEnumerator LockMovementBriefly()
    {
        setLocked(true);

        yield return new WaitForSecondsRealtime(spawnLockSeconds);

        setLocked(false);
        lockRoutine = null;
        RaiseHint("spawnLockEnded");
    }

    private void setLocked(bool shouldLock)
    {
        if (Services != null && Services.Movement != null)
        {
            Services.Movement.SetMovementLocked(shouldLock);
        }
    }

    private void stopLock()
    {
        if (lockRoutine != null)
        {
            StopCoroutine(lockRoutine);
            lockRoutine = null;
        }
    }
}
