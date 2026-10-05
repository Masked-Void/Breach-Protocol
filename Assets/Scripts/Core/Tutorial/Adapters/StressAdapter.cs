using UnityEngine;
using System;

// connects the tutorial to HeartbeatManager, which lives in Bootstrap. the only tutorial file that changes when the heartbeat does
public class StressAdapter : MonoBehaviour, ITutorialStress
{
    private bool isTutorialModeOn;

    public event Action HeartFailed;

    public int CurrentBpm => HeartbeatManager.instance != null ? HeartbeatManager.instance.CurrentBpm : 0;

    private void OnEnable()
    {
       
    }

    private void OnDestroy()
    {
        if (isTutorialModeOn)
        {
            SetTutorialMode(false);
        }    
    }

    public void ResetToResting()
    {
        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.ResetToRestingBpm();
        }
    }

    public void SetTutorialMode(bool isOn)
    {
        isTutorialModeOn = isOn;

        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.SetLoseSuppressed(isOn);
        }
    }

    private void handleHeartFailed()
    {
        if (isTutorialModeOn)
        {
            HeartFailed?.Invoke();
        }
    }
}
