using System;
using System.Collections;
using UnityEngine;


// One place the boss fight talks to for traps. BossFightManager calls a phase number in and
// this sets the lasers, lava and platforms to that phase's settings, so the fight script never
// has to know how any individual traps work.
public class TrapManager : MonoBehaviour {

    [Header("References")]
    [SerializeField] private LaserArrayManager laserManager;
    [SerializeField] private LavaManager lavaManager;
    [SerializeField] private PlatformManager platManager;

    [Header("Phase 1")]
    [SerializeField] private trapSetup p1;

    [Header("Immune 1 (P1 to P2)")]
    [SerializeField] private trapSetup p1_p2;

    [Header("Phase 2")]
    [SerializeField] private trapSetup p2;

    [Header("Immune 2 (P2 to P3)")]
    [SerializeField] private trapSetup p2_p3;

    [Header("Phase 3")]
    [SerializeField] private trapSetup p3;

    [Header("Immune 3 (P3 to P4)")]
    [SerializeField] private trapSetup p3_p4;

    [Header("Phase 4")]
    [SerializeField] private trapSetup p4;

    // What is running so applying the same thing does nothing
    private int currentPattern = int.MinValue;
    private bool generatedRunning = false;
    private float currentDifficulty = -1f;
    private int currentSpin = 0;
    private bool platformsUp = false;


    private Coroutine cycleRoutine;


    void Awake() {
        // Logs potential fatal errors
        if (laserManager == null) {
            Debug.LogWarning("TrapManager: no LaserArrayManager assigned" , this);
        }
        if (lavaManager == null) {
            Debug.LogWarning("TrapManager: no LavaManager assigned" , this);
        }
        if (platManager == null) {
            Debug.LogWarning("TrapManager: no platformsManager assigned" , this);
        }

    }


    // Called by BossFightManager as each phase begins or ends
    public void startP1() { applySetup(p1); }
    public void startP2() { applySetup(p2); }
    public void startP3() { applySetup(p3); }
    public void startP4() { applySetup(p4); }

    public void endP1() { applySetup(p1_p2); }
    public void endP2() { applySetup(p2_p3); }

    public void endP3() { applySetup(p3_p4); }

    public void endP4() { stopAll(); }



    // Swaps traps to new segments settings
    private void applySetup(trapSetup setup) {
        stopCycle();

        activateLasers(setup);
        if (setup.cycleTraps) {
            cycleRoutine = StartCoroutine(trapCycle(setup));
        } else {
            activateLava(setup);
            activatePlatforms(setup);
        }
    }

    private void stopCycle() {
        if (cycleRoutine == null)
            return;

        StopCoroutine(cycleRoutine);
        cycleRoutine = null;
    }


    [ContextMenu("Stop all traps")]
    public void stopAll() {
        stopCycle();

        if (laserManager != null) {
            laserManager.stopPattern();
            laserManager.stopRotation();
        }

        if (lavaManager != null) {
            lavaManager.drain();
        }

        if (platManager != null) {
            platManager.fallPlatforms();
        }

        currentPattern = -1;
        generatedRunning = false;
        currentDifficulty = -1f;
        currentSpin = 0;
        platformsUp = false;
    }

    // Cycles lava and platforms for interesting gameplay will probably be used in p4
    private IEnumerator trapCycle(trapSetup setup) {
        bool up = false;

        while (true) {
            up = !up;

            if (setup.platformsActive && platManager != null) {
                if (up)
                    platManager.risePlatforms();
                else
                    platManager.fallPlatforms();

                platformsUp = up;
            }

            if (setup.lavaActive && lavaManager != null) {
                lavaManager.moveTo(up ? setup.lavaLevel : 0);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f , setup.cycleInterval));
        }
    }

    private void activateLasers(trapSetup setup) {
        if (laserManager == null) {
            return;
        }

        if (!setup.laserActive) {
            if (generatedRunning || currentPattern != -1) {
                laserManager.stopPattern();
                generatedRunning = false;
                currentDifficulty = -1f;
                currentPattern = -1;
            }

            if (currentSpin != 0) {
                laserManager.stopRotation();
                currentSpin = 0;
            }

            return;
        }

        if (setup.generatePattern) {
            if (!generatedRunning || !Mathf.Approximately(currentDifficulty , setup.laserDifficulty)) {
                laserManager.startGeneratedPattern(setup.laserDifficulty);
                generatedRunning = true;
                currentDifficulty = setup.laserDifficulty;
                currentPattern = int.MinValue;
            }
        } else if (generatedRunning || setup.laserPattern != currentPattern) {
            if (setup.laserPattern >= 0) {
                laserManager.startPattern(setup.laserPattern);
            } else {
                laserManager.stopPattern();
            }

            generatedRunning = false;
            currentDifficulty = -1f;
            currentPattern = setup.laserPattern;
        }

        if (setup.laserSpinDirection != currentSpin) {
            if (setup.laserSpinDirection != 0) {
                laserManager.startSpin(setup.laserSpinDirection);
            } else {
                laserManager.stopRotation();
            }

            currentSpin = setup.laserSpinDirection;
        }
    
    }

    private void activateLava(trapSetup setup) {
        if (lavaManager == null)
            return;

        lavaManager.moveTo(setup.lavaActive ? setup.lavaLevel : 0f);
    }
    private void activatePlatforms(trapSetup setup) {
        if (platManager == null)
            return;

        if (setup.platformsActive == platformsUp)
            return;

        if (setup.platformsActive)
            platManager.risePlatforms();
        else
            platManager.fallPlatforms();

        platformsUp = setup.platformsActive;

    }



    [ContextMenu("Apply defaults")]
    private void applyDefaults() {
        // Phase 1, enemies only, no hazards at all
        p1 = makeSetup(false , -1 , false , 0f , 0 , false , 0f , false , false , 0f);

        // Immune 1, lasers spin and rise and fall
        p1_p2 = makeSetup(true , -1 , true , 0.15f , 1 , false , 0f , false , false , 0f);

        // Phase 2, the lasers stay active
        p2 = makeSetup(true , -1 , true , 0.15f , 1 , false , 0f , false , false , 0f);

        // Immune 2, more lasers and the platforms rise so the hold point goes up
        p2_p3 = makeSetup(true , -1 , true , 0.5f , 1 , false , 0f , true , false , 0f);

        // Phase 3, more lasers again. Platforms stay up since the escalation is cumulative
        p3 = makeSetup(true , -1 , true , 0.5f , 1 , false , 0f , true , false , 0f);

        // Immune 3, platforms rise with lava behind them
        p3_p4 = makeSetup(true , -1 , true , 0.8f , -1 , true , 0.6f , true , false , 0f);

        // Phase 4, platforms and lava rise and fall on a loop
        p4 = makeSetup(true , -1 , true , 1f , -1 , true , 0.85f , true , true , 8f);
    }

    // Makes a setup based on given values
    private trapSetup makeSetup(bool lasers , int pattern , bool generate , float difficulty , int spin , bool lavaOn , float level , bool platforms , bool cycle , float interval) {
        trapSetup setup = new trapSetup();

        setup.laserActive = lasers;
        setup.laserPattern = pattern;
        setup.generatePattern = generate;
        setup.laserDifficulty = difficulty;
        setup.laserSpinDirection = spin;
        setup.lavaActive = lavaOn;
        setup.lavaLevel = level;
        setup.platformsActive = platforms;
        setup.cycleTraps = cycle;
        setup.cycleInterval = interval;

        return setup;
    }

    [ContextMenu("Test Phase 1")]
    void testP1() { startP1(); }

    [ContextMenu("Test Immune 1")]
    void testP1P2() { endP1(); }

    [ContextMenu("Test Phase 2")]
    void testP2() { startP2(); }

    [ContextMenu("Test Immune 2")]
    void testP2P3() { endP2(); }

    [ContextMenu("Test Phase 3")]
    void testP3() { startP3(); }

    [ContextMenu("Test Immune 3")]
    void testP3P4() { endP3(); }

    [ContextMenu("Test Phase 4")]
    void testP4() { startP4(); }
}
