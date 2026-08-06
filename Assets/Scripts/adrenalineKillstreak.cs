using UnityEngine;

public class adrenalineKillstreak : killstreakBase
{
    [Header("Adrenaline Rush")]
    [Tooltip("How slow the world becomes. 0.2 = enemies move at 20% speed.")]
    [SerializeField][Range(0.01f, 1f)] private float worldTimeScale = 0.2f;

    protected override void onActivate()
    {
        if (timeManager.instance != null)
        {
            timeManager.instance.setTimeScaleOverride(worldTimeScale);
        }
    }

    protected override void onDeactivate()
    {
        if (timeManager.instance != null)
        {
            timeManager.instance.clearTimeScaleOverride();
        }
    }
}