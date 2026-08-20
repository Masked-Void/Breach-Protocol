using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// owns every hold point in the boss arena. runs the center point during immune phases and
// lights up one random outer point during damage phases. zones report back here instead of
// reaching into the boss themselves.
public class holdZoneManager : MonoBehaviour {
    [Header("Zones")]
    [Tooltip("the single center point used by all three immune phases")]
    [SerializeField] private holdZone immuneZone;
    [Tooltip("the 3 or 4 optional damage points. one gets picked at random each damage phase")]
    [SerializeField] private List<holdZone> damageZones = new List<holdZone>();


    [Header("Damage holds")]
    [Tooltip("On means a fresh point opens after one finishes")]
    [SerializeField] private bool repeatDamageHolds = true;

    [Header("References")]
    [SerializeField] private bossFightManager boss;

    [Header("HUD")]
    [Tooltip("optional hud mirror of whichever point is live right now. the computer is still the main readout")]
    [SerializeField] private Image hudFill;
    [SerializeField] private GameObject hudObj;

    // Phase transition polls these
    public bool immuneHoldDone { get; private set; }
    public float immuneProgress { get { return immuneZone != null ? immuneZone.progress : 0f; } }
    public bool holdActive { get { return activeZone != null; } }
    public int damageHoldsDone { get; private set; }

    public bool hasImmuneZone { get { return immuneZone != null; } }

    // Cleaned copy of the list
    private holdZone[] damagePoints;
    private holdZone activeZone;
    private int lastDamageIndex = -1;

    void Awake() {
        if (boss == null)
            boss = GetComponentInParent<bossFightManager>();

        if (boss == null) { Debug.LogError("holdZoneManager has no bossFightManager" , this); }
        if (immuneZone == null) { Debug.LogError("holdZoneManager has no immune zone assigned" , this); }

        buildDamagePoints();

        if (hudObj != null)
            hudObj.SetActive(false);
    }

    void buildDamagePoints() {
        int counted = 0;

        for (int i = 0 ; i < damageZones.Count ; i++) {
            if (damageZones[i] != null)
                counted++;
        }

        damagePoints = new holdZone[counted];

        int writen = 0;

        for (int i = 0 ; i < damageZones.Count ; i++) {
            if (damageZones[i] == null)
                continue;

            damagePoints[writen] = damageZones[i];
            writen++;
        }

        if (counted == 0) {
            Debug.LogWarning("holdZoneManager: no damage points assigned" , this);
        }
    }
    void Update() {
        if (activeZone == null)
            return;
        if (hudFill != null)
            hudFill.fillAmount = activeZone.progress;
    }

    // called from the phase transition. the boss stays immune until this hold finishes
    [ContextMenu("Start Immune Hold")]
    public void startImmuneHold() {
        stopAll();

        immuneHoldDone = false;

        if (immuneZone == null){
            Debug.LogError("holdZoneManager: startImmuneHold with no immune zone assigned" , this);
            return;
        }
        activeZone = immuneZone;
        immuneZone.activate(this);

        if (hudObj != null)
            hudObj.SetActive(true);
    }

    // called at the start of a damage phase. picks a point that isn't the one from last time
    [ContextMenu("Start Damage Hold")]
    public void startDamageHold() {
        stopAll();
        
        if (damagePoints == null || damagePoints.Length == 0)
            return;

        int pick;
        if (damagePoints.Length > 1 && lastDamageIndex>=0) {
            // roll from the list minus last time's point, then step over the gap.
            // doing it this way keeps every remaining point equally likely
            pick = Random.Range(0 , damagePoints.Length - 1);
            if (pick >= lastDamageIndex)
                pick++;
        } else {
            pick = Random.Range(0 , damagePoints.Length);
        }

        lastDamageIndex = pick;
        activeZone = damagePoints[pick];
        if (activeZone == null)
            return;

        activeZone.activate(this);
        if (hudObj != null)
            hudObj.SetActive(true);
    }

    // shuts down every point, not just the live one, so nothing survives a phase handoff
    [ContextMenu("Stop All Holds")]
    public void stopAll() {
        if (immuneZone != null)
            immuneZone.deactivate();

        for (int i = 0 ; i < damagePoints.Length ; i++)
            if (damagePoints[i] != null)
                damagePoints[i].deactivate();

        activeZone = null;
        if (hudObj != null)
            hudObj.SetActive(false);
    }

    public void holdComplete(holdZone zone) {

        if (zone == null)
            return;

        if (zone == immuneZone) {
            immuneHoldDone = true;
            return;
        }

        if (boss != null)
            boss.applyDamage(zone.damageAmt);

        damageHoldsDone++;

        if (repeatDamageHolds)
            startDamageHold();

        else
            stopAll();
    }

    [ContextMenu("Force Break Immunity")]
    public void forceCompleteImmune() {
        immuneHoldDone = true;
    }

    [ContextMenu("Force Damage Payout")]
    void debugPayout() {
        if (activeZone == null || activeZone == immuneZone)
            return;

        holdComplete(activeZone);
    }
}