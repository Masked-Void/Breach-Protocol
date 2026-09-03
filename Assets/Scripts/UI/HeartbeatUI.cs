using System.Collections;
using TMPro;
using UnityEngine;

/*
 * Script: HeartbeatUI
 *
 * Description:
 * Drives the heart image in the HUD, beating faster as the player's BPM rises.
 *
 * Interacts With:
 * - HeartbeatManager (reads CurrentBpm)
 */



public class HeartbeatUI : MonoBehaviour
{
    [Tooltip("the heart image, scaled up and back on each beat")]
    [SerializeField] RectTransform heartImage;

    [Tooltip("numeric bpm readout next to the heart")]
    [SerializeField] TextMeshProUGUI bpmText;

    [Tooltip("how big the heart gets at the peak of a beat, 1.2 is 20 percent")]
    [SerializeField] float pulseScale = 1.2f;

    [Tooltip("seconds one pulse takes, the gap between beats comes from bpm")]
    [SerializeField] float pulseDuration = .2f;

    // seconds since the last beat, unscaled so the heart keeps time while the
    // world is slowed
    float beatTimer;

    // scale to return to after each pulse, captured on Start
    Vector3 origHeartScale;

    // stops a second pulse starting before the first finishes
    bool isPulsing;


    void Start()
    {
        origHeartScale = heartImage.localScale;
    }

    void Update()
    {

        if (HeartbeatManager.instance == null)
        {
            return;
        }

        bpmText.text = HeartbeatManager.instance.CurrentBpm + " BPM";

        int bpm = HeartbeatManager.instance.CurrentBpm;

        float beatInterval = 60f / bpm;

        beatTimer += Time.unscaledDeltaTime;


        if (beatTimer >= beatInterval && !isPulsing)
        {
            beatTimer -= beatInterval;
            StartCoroutine(Pulse());
            //Debug.Log(Time.time);
        }
    }

    // scales the heart up then back down. real seconds, so the beat stays
    // steady regardless of world time scale.
    IEnumerator Pulse()
    {
        isPulsing = true;

        heartImage.localScale = origHeartScale * pulseScale;

        yield return new WaitForSecondsRealtime(pulseDuration);

        heartImage.localScale = origHeartScale;

        isPulsing = false;
    }
}
