using UnityEngine;

/// <summary>
/// OVERCLOCK: the player remains on real-time controls while the world/enemies stay slow.
/// </summary>
public class overclockKillstreak : killstreakBase
{
    [Header("Overclock")]
    [SerializeField, Range(0.01f, 1f)] private float worldTimeScale = 0.20f;

    protected override void onActivate()
    {
        if (timeManager.instance != null)
            timeManager.instance.setTimeScaleOverride(worldTimeScale);
    }

    protected override void onDeactivate()
    {
        if (timeManager.instance != null)
            timeManager.instance.clearTimeScaleOverride();
    }

    private void Reset()
    {
        killstreakName = "Overclock";
        duration = 8f;
    }
}

