using UnityEngine;

// ring 1. walking pushes bpm past the spike line, then the player stands still until it drops under the calm line.
// done once both happen, in that order
public class BpmRingLesson : TutorialLesson
{
    public enum Phase
    {
        WaitingForSpike,
        WaitingForCalm,
        Done
    }

    [Header("Lines")]
    [SerializeField] private int spikeBpm = 80;

    [SerializeField] private int calmBpm = 40;

    private Phase phase = Phase.WaitingForSpike;

    public Phase CurrentPhase => phase;

    public override TutorialStage Stage => TutorialStage.BpmRing;

    private void Update()
    {
        if (!IsRunning || phase == Phase.Done || Services == null || Services.Stress == null)
        {
            return;
        }

        int bpm = Services.Stress.CurrentBpm;

        if (phase == Phase.WaitingForSpike && bpm >= spikeBpm)
        {
            phase = Phase.WaitingForCalm;
            RaiseHint("spikeReached");
            return;
        }

        if (phase == Phase.WaitingForCalm && bpm <= calmBpm)
        {
            phase = Phase.Done;
            RaiseHint("calmReached");
            Complete();
        }
    }

    public override void BeginLesson()
    {
        base.BeginLesson();
        phase = Phase.WaitingForSpike;

        if (Services != null && Services.Stress != null)
        {
            Services.Stress.ResetToResting();
        }
    }

    public override void ResetLesson()
    {
        base.ResetLesson();
        phase = Phase.WaitingForSpike;
    }
}
