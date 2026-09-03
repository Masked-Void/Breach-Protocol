using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script: HoldZoneManager
 *
 * Description:
 * Owns every hold point in the boss arena. Runs the centre point during immune
 * phases and lights one random outer point during damage phases. Zones report
 * back here instead of reaching into the boss themselves.
 *
 * Responsibilities:
 * - Start and stop the immune hold that breaks boss invulnerability
 * - Pick a random damage point each damage phase, never the same one twice running
 * - Mirror the live point's progress on the HUD
 * - Shut every point down on a phase handoff
 *
 * Interacts With:
 * - HoldZone (the individual points)
 * - BossFightManager (polls immuneHoldDone and immuneProgress)
 *
 * Notes:
 * - The damage point roll draws from the list minus last time's index, then
 *   steps over the gap. That keeps every remaining point equally likely instead
 *   of rerolling until it differs.
 */
public class HoldZoneManager : MonoBehaviour
{
    [Header("Zones")]
    [Tooltip("the single center point used by all three immune phases")]
    [SerializeField] private HoldZone immuneZone;
    [Tooltip("the 3 or 4 optional damage points. one gets picked at random each damage phase")]
    [SerializeField] private List<HoldZone> damageZones = new List<HoldZone>();


    [Header("Damage holds")]
    [Tooltip("On means a fresh point opens after one finishes")]
    [SerializeField] private bool repeatDamageHolds = true;

    [Header("References")]
    [Tooltip("the fight this belongs to, found on a parent if left empty")]
    [SerializeField] private BossFightManager boss;

    [Header("HUD")]
    [Tooltip("optional hud mirror of whichever point is live right now. the computer is still the main readout")]
    [SerializeField] private Image hudFill;

    [Tooltip("root of the hud readout, hidden whenever no point is live")]
    [SerializeField] private GameObject hudObj;


    // BossFightManager polls these each frame to decide when a phase can end
    public bool immuneHoldDone { get; private set; }
    public float immuneProgress { get { return immuneZone != null ? immuneZone.progress : 0f; } }
    public bool holdActive { get { return activeZone != null; } }
    public int damageHoldsDone { get; private set; }

    public bool hasImmuneZone { get { return immuneZone != null; } }

    // Cleaned copy of the list
    private HoldZone[] damagePoints;
    private HoldZone activeZone;
    private int lastDamageIndex = -1;

    void Awake() {
        if (boss == null)
            boss = GetComponentInParent<BossFightManager>();

        if (boss == null) { Debug.LogError("HoldZoneManager has no BossFightManager" , this); }
        if (immuneZone == null) { Debug.LogError("HoldZoneManager has no immune zone assigned" , this); }

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

        damagePoints = new HoldZone[counted];

        int writen = 0;

        for (int i = 0 ; i < damageZones.Count ; i++) {
            if (damageZones[i] == null)
                continue;

            damagePoints[writen] = damageZones[i];
            writen++;
        }

        if (counted == 0) {
            Debug.LogWarning("HoldZoneManager: no damage points assigned" , this);
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
    public void StartImmuneHold() {
        StopAll();

        immuneHoldDone = false;

        if (immuneZone == null){
            Debug.LogError("HoldZoneManager: startImmuneHold with no immune zone assigned" , this);
            return;
        }
        activeZone = immuneZone;
        immuneZone.Activate(this);

        if (hudObj != null)
            hudObj.SetActive(true);
    }

    // called at the start of a damage phase. picks a point that isn't the one from last time
    [ContextMenu("Start Damage Hold")]
    public void StartDamageHold() {
        StopAll();
        
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

        activeZone.Activate(this);
        if (hudObj != null)
            hudObj.SetActive(true);
    }

    // shuts down every point, not just the live one, so nothing survives a phase handoff
    [ContextMenu("Stop All Holds")]
    public void StopAll() {
        if (immuneZone != null)
            immuneZone.Deactivate();

        if (damagePoints != null) {
            for (int i = 0 ; i < damagePoints.Length ; i++)
                if (damagePoints[i] != null)
                    damagePoints[i].Deactivate();
        }
        activeZone = null;
        if (hudObj != null)
            hudObj.SetActive(false);
    }

    public void HoldComplete(HoldZone zone) {

        if (zone == null)
            return;

        if (zone == immuneZone) {
            immuneHoldDone = true;
            return;
        }

        if (boss != null)
            boss.ApplyDamage(zone.damageAmt);

        damageHoldsDone++;

        if (repeatDamageHolds)
            StartDamageHold();

        else
            StopAll();
    }

    [ContextMenu("Force Break Immunity")]
    public void ForceCompleteImmune() {
        immuneHoldDone = true;
    }

    [ContextMenu("Force Damage Payout")]
    void debugPayout() {
        if (activeZone == null || activeZone == immuneZone)
            return;

        HoldComplete(activeZone);
    }
}