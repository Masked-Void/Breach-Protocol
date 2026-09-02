using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// runs the CEO fight. owns boss health, the four phases, and the immune windows between them.
// immunity is broken by the player finishing a hold, so the immune window waits on HoldZoneManager.
public class BossFightManager : MonoBehaviour,IDamage {

    [Header("References")]
    [SerializeField] private GameObject boss;
    [SerializeField] private BossWaveManager bossWavesManager;
    [SerializeField] private HoldZoneManager holdManager;
    [SerializeField] private TrapManager trapManager;

    [Header("UI")]
    [SerializeField] private GameObject immuneBarObj;
    [SerializeField] private GameObject healthBarObj;
    [SerializeField] private Image healthBar;
    [Tooltip("fills 0 to 1 as the hold progresses, so set the art to fill in that direction")]
    [SerializeField] private Image immuneBar;

    [Header("Health")]
    [SerializeField] private float maxHealth = 1000f;
    [Range(0f , 10f)][SerializeField] private float bulletDamageMult = 1f;
    [Range(0f , 1f)][SerializeField] private float p1EndHealthPerc = .75f;
    [Range(0f , 1f)][SerializeField] private float p2EndHealthPerc = .5f;
    [Range(0f , 1f)][SerializeField] private float p3EndHealthPerc = .25f;

    [Header("Misc")]
    [Tooltip("Files banked for killing the CEO")]
    [SerializeField] private int bossFileReward = 5;

    [Header("Debug")]
    [Tooltip("tick this in play mode to chip the boss by damageAmt")]
    [SerializeField] private bool dealDamage = false;
    [SerializeField] private float damageAmt = 10f;

    // the wave manager reads these
    [System.NonSerialized] public List<bool> phaseActive = new List<bool> { false , false , false , false };
    [System.NonSerialized] public List<bool> phaseActivated = new List<bool> { false , false , false , false };
    [System.NonSerialized] public List<bool> phaseImmuned = new List<bool> { false , false , false , false };
    [System.NonSerialized] public List<float> endHealthReqs = new List<float> { 0f , 0f , 0f , 0f };

    [System.NonSerialized] public bool isImmune;
    [System.NonSerialized] public int phase = 0;

    private float curHealth;
    private bool fightOver = false;
    private MaskShake shake;


    void Awake() {
        // every one of these is fatal, so bail out instead of limping into a null dereference later.
        // returning matters here, enabled = false on its own doesn't stop the rest of this method
        if (boss == null) {
            Debug.LogError("BossFightManager: no boss object assigned" , this);
            enabled = false;
            return;
        }

        if (bossWavesManager == null) {
            Debug.LogError("BossFightManager: no BossWaveManager assigned" , this);
            enabled = false;
            return;
        }

        if (holdManager == null) {
            Debug.LogError("BossFightManager: no HoldZoneManager assigned" , this);
            enabled = false;
            return;
        }

        if (trapManager == null) {
            Debug.LogError("BossFightManager: no TrapManager assigned" , this);
            enabled = false;
            return;
        }

        // health thresholds that end each phase. phase 4 ends on death so its req stays at 0
        endHealthReqs[0] = p1EndHealthPerc * maxHealth;
        endHealthReqs[1] = p2EndHealthPerc * maxHealth;
        endHealthReqs[2] = p3EndHealthPerc * maxHealth;
        endHealthReqs[3] = 0f;

        curHealth = maxHealth;

        isImmune = false;
        phase = 0;

        // the mask is a child of the boss, so find it by name before pulling components off it
        Transform mask = findMark("Head");
        if (mask == null) {
            Debug.LogError("BossFightManager: no child named Head on the boss object" , this);
            enabled = false;
            return;
        }

        if (!mask.TryGetComponent(out shake)) {
            Debug.LogError("BossFightManager: no MaskShake component on the boss head" , this);
        }

        if (healthBarObj != null) {
            healthBarObj.SetActive(true);
        }
    }


    void Start() {
        phase = 0;
        isImmune = false;
        phaseActive[0] = true;
        startPhase(0);
    }


    void Update() {
        // once the boss is down nothing else should tick
        if (fightOver)
            return;

        // debug toggle. clear the flag whether or not the hit landed, otherwise a tick during
        // immunity sits queued and fires the instant immunity drops
        if (dealDamage) {
            ApplyDamage(damageAmt);
            dealDamage = false;
        }

        // phase ends when health crosses its threshold. phaseActivated keeps it from firing twice
        if (!isImmune && !phaseActivated[phase] && curHealth <= endHealthReqs[phase]) {
            phaseActivated[phase] = true;
            StartCoroutine(phaseTransition(phase));
        }

        updateBossUI();
    }


    void updateBossUI() {

        if (healthBar==null) {
            return;
        }

        healthBar.fillAmount = Mathf.Clamp01(curHealth / Mathf.Max(1f , maxHealth));
        
    }


    // single entry point for damage. guns, holds, and the debug toggle all come through here
    public void ApplyDamage(float amt) {
        if (fightOver || isImmune)
            return;

        curHealth = Mathf.Max(0f , curHealth - amt);
    }


    IEnumerator phaseTransition(int endingPhase) {
        phaseActive[endingPhase] = false;

        bool lastPhase = endingPhase >= phaseActive.Count - 1;

        if (!lastPhase) {
            // shut down the waves the phase was running
            if (endingPhase == 0) {
                bossWavesManager.EndP1();
                trapManager.EndP1();
            } else if (endingPhase == 1) {
                bossWavesManager.EndP2();
                trapManager.EndP2();
            } else if (endingPhase == 2) {
                bossWavesManager.EndP3();
                trapManager.EndP3();
            }
            isImmune = true;
            phaseImmuned[endingPhase] = true;
            if (immuneBarObj != null) {
                immuneBarObj.SetActive(true);
            }

            // shaking mask is the immune tell, set it once instead of re-checking it every frame
            if (shake != null)
                shake.doShake = true;

            // no timer here. the window stays open until the player finishes the center hold
            holdManager.StartImmuneHold();

            if (!holdManager.hasImmuneZone) {
                Debug.LogError("BossFightManager: no immune zone, skipping hold" , this);
            } else {
                while (!holdManager.immuneHoldDone) {
                    if (immuneBar != null) {
                        immuneBar.enabled = true;
                        immuneBar.fillAmount = holdManager.immuneProgress;
                    }
                    yield return null;
                }
            }

            holdManager.StopAll();

            if (shake != null)
                shake.doShake = false;

            if(immuneBar != null) {
                immuneBar.fillAmount = 0f;
            }

            if (immuneBarObj != null) { 
                immuneBarObj.SetActive(false);
            }

            phaseImmuned[endingPhase] = false;
            isImmune = false;

            // phase only advances at the end of the handoff, not when the threshold was crossed
            phase = endingPhase + 1;
            phaseActive[phase] = true;
            startPhase(phase);
        } else {
            fightOver = true;
            holdManager.StopAll();
            bossDefeated();
        }
    }


    void bossDefeated() {

        Debug.Log("boss fight completed");

        // Stops wave and traps
        bossWavesManager.EndP4();
        trapManager.EndP4();

        if (GameManager.instance != null) {
            GameManager.instance.AddFiles(bossFileReward);
            GameManager.instance.StateWin();
        }

        if (healthBarObj != null) {
            healthBarObj.SetActive(false);
        }

    }


    void startPhase(int p) {

        // every damage phase gets one optional hold point picked at random
        holdManager.StartDamageHold();

        switch (p) {
            case 0:
                Phase1();
                break;
            case 1:
                Phase2();
                break;
            case 2:
                Phase3();
                break;
            case 3:
                Phase4();
                break;
        }
    }


    public void Phase1() {
        bossWavesManager.StartP1();
        trapManager.StartP1();
    }

    public void Phase2() {
        bossWavesManager.StartP2();
        trapManager.StartP2();
    }

    public void Phase3() {
        bossWavesManager.StartP3();
        trapManager.StartP3();
    }

    public void Phase4() {
        bossWavesManager.StartP4();
        trapManager.StartP4();
    }

    public void TakeDamage(int amount) {
        ApplyDamage(amount * bulletDamageMult);
    }

    // Looks through the bosses object's children for a marker with an exact name match
    Transform findMark(string wanted)
    {
        // Goes through each child of the boss object
        foreach (Transform child in boss.transform)
        {
            if (child.name.IndexOf(wanted , System.StringComparison.OrdinalIgnoreCase) >= 0) {

                return child;
            }
        }

        // nothing matched, Awake handles that
        return null;

    }
}
