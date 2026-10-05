using System;

// what the tutorial needs from the heartbeat system
public interface ITutorialStress
{
    // current bpm, resting to max
    int CurrentBpm { get; }

    // raised at max bpm while tutorial mode is on, the tutorial restarts the space instead of the run ending
    event Action HeartFailed;

    // puts bpm back to resting, for space restarts and ring 1's start
    void ResetToResting();

    // while on, max bpm raises HeartFailed and skips the lose state
    void SetTutorialMode(bool isOn);
}
