using UnityEngine;

/// <summary>
/// OVERCLOCK: the player remains on real-time controls while the world/enemies stay slow.
/// </summary>
public class OverclockKillstreak : KillstreakBase
{
    [Header("Overclock")]
    [Tooltip("world speed while active, the player stays at full speed so they move faster than everything")]
    [SerializeField, Range(0.01f, 1f)] private float worldTimeScale = 0.20f;

    protected override void onActivate()
    {
        if (TimeManager.instance != null)
            TimeManager.instance.SetTimeScaleOverride(worldTimeScale);
    }

    protected override void onDeactivate()
    {
        if (TimeManager.instance != null)
            TimeManager.instance.ClearTimeScaleOverride();
    }

    private void Reset()
    {
        killstreakName = "Overclock";
        duration = 8f;
    }
}

