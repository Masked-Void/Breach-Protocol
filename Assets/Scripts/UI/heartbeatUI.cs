
/*
 * Script heartBeatUI
 * Author: Mark Fittante
 * 
 * Takes player heartbeat and changes speed of heart image in UI.
 * 
 * 
 * Communicates with heartBeatManager
 * 
 * 
 * 
 */









using UnityEngine;
using System.Collections;
using TMPro;




public class heartbeatUI : MonoBehaviour
{
    [SerializeField] RectTransform heartImage;
    [SerializeField] TextMeshProUGUI bpmText;

    [SerializeField] float pulseScale = 1.2f;
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

        if (heartbeatManager.instance == null)
        {
            return;
        }

        bpmText.text = heartbeatManager.instance.getCurrentBPM() + " BPM";

        int bpm = heartbeatManager.instance.getCurrentBPM();

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
