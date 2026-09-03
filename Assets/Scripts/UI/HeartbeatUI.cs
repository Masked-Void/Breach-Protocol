
/*
 * Script: HeartbeatUI
 *
 * Description:
 * Drives the heart image in the HUD, beating faster as the player's BPM rises.
 *
 * Interacts With:
 * - HeartbeatManager (reads CurrentBpm)
 */

using UnityEngine;
using System.Collections;
using TMPro;


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

    float beatTimer;
    Vector3 origHeartScale;
    bool isPulsing;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        origHeartScale = heartImage.localScale;
    }

    // Update is called once per frame
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

    IEnumerator Pulse()
    {
        isPulsing = true;

        heartImage.localScale = origHeartScale * pulseScale;

        yield return new WaitForSecondsRealtime(pulseDuration);

        heartImage.localScale = origHeartScale;

        isPulsing = false;
    }
}
