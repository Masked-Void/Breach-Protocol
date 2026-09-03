using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script: LaserArrayManager
 *
 * Description:
 * Coordinates the four laser pillars in the boss arena. Owns firing patterns,
 * pillar rotation, and the procedural pattern generator that scales with
 * difficulty.
 *
 * Responsibilities:
 * - Resolve a colour and slot to a specific LaserArray and local index
 * - Fire and stop individual lasers, whole pillars, or everything
 * - Spin and sweep the pillar rig, with a shared rotation handle
 * - Run authored patterns and generate new ones at a given difficulty
 *
 * Interacts With:
 * - LaserArray (the individual pillars)
 * - laserStep, laserPattern, patternGenerator (pattern data)
 * - pillarColor (which pillar a step targets)
 * - BossFightManager, TrapManager (start and stop patterns per phase)
 *
 * Notes:
 * - Runs on unscaled time. Boss hazards ignore the player's time scale.
 * - Debug logging is on by default and fires during normal play. Consider
 *   gating it behind a bool if the console gets noisy.
 */
public class LaserArrayManager : MonoBehaviour {

    [Header("Pillar Wiring")]
    [Tooltip("lasers on the Red pillar, ordered bottom to top. size is per pillar, they dont have to match")]
    [SerializeField] private LaserArray[] pillarRed;

    [Tooltip("lasers on the Blue pillar, ordered bottom to top")]
    [SerializeField] private LaserArray[] pillarBlue;

    [Tooltip("lasers on the Green pillar, ordered bottom to top")]
    [SerializeField] private LaserArray[] pillarGreen;

    [Tooltip("lasers on the Yellow pillar, ordered bottom to top")]
    [SerializeField] private LaserArray[] pillarYellow;


    [Header("Rotation")]
    [Tooltip("degrees per second for the continuous spin. direction comes from the startSpin call, not from here")]
    [SerializeField] private float spinSpeed = 45f;

    [Tooltip("degrees per second when sweeping to a set angle. usually faster than the ambient spin")]
    [SerializeField] private float sweepSpeed = 120f;

    [Tooltip("how close to the target angle counts as arrived. keep it above one frame of travel or it overshoots forever")]
    [SerializeField] private float angleEpsilon = 0.5f;

    // every array that rotates with the rig, and the rotation each started at.
    // the spin is applied as an offset from those, so a designer can angle a
    // pillar in the scene and the spin still reads correctly.
    private LaserArray[] spinTargets;
    private Quaternion[] spinBaseRots;
    private float spinAngle;

    [Header("Patterns")]
    [Tooltip("all the firing sequences. the boss controller calls these by index")]
    [SerializeField] private laserPattern[] patterns;

    [Tooltip("settings for building a pattern on the fly instead of using an authored one")]
    [SerializeField] private patternGenerator generator = new patternGenerator();

    // built in awake from the four fields above. unity wont serialize a jagged array
    // but nothing stops us making one at runtime, thats the whole reason the fields are split
    private LaserArray[][] pillars;

    // two handles so the group can spin while a pattern is firing
    private Coroutine rotateRoutine;
    private Coroutine patternRoutine;



    // stitch the four inspector fields into something indexable
    // has to match the pillarColor enum order or the colors get swapped
    private void Awake() {

        pillars = new LaserArray[4][];

        pillars[0] = pillarRed;
        pillars[1] = pillarBlue;
        pillars[2] = pillarGreen;
        pillars[3] = pillarYellow;

        for (int i = 0 ; i < pillars.Length ; i++) {
            if (pillars[i] == null || pillars[i].Length == 0) {
                Debug.LogWarning("LaserArrayManager: the "+(pillarColor)i+" pillar has no laserArrays, it will be skipped",this);
            }
        }

        buildSpinTargets();
    }

    // collects everything the spin rotates and remembers where each started
    private void buildSpinTargets() {
        
        List<LaserArray> found = new List<LaserArray>();

        for (int pillarIndex = 0 ; pillarIndex < pillars.Length ; pillarIndex++) {
            if (pillars[pillarIndex] == null) {
                continue;
            }

            for (int laserIndex = 0 ; laserIndex < pillars[pillarIndex].Length ; laserIndex++) {
                if (pillars[pillarIndex][laserIndex] != null) {
                    found.Add(pillars[pillarIndex][laserIndex]);
                }
            }
        }

        spinTargets = found.ToArray();
        spinBaseRots = new Quaternion[spinTargets.Length];

        for (int i = 0 ; i < spinTargets.Length ; i++) {
            spinBaseRots[i] = spinTargets[i].transform.localRotation;
        }

        spinAngle = 0f;
    }

    // collects everything the spin rotates and remembers where each started
    private void applySpinAngle() {
        Quaternion offset = Quaternion.Euler(0f , spinAngle , 0f);

        for (int i = 0 ; i < spinTargets.Length ; i++) {
            if (spinTargets[i] != null) {
                spinTargets[i].transform.localRotation = spinBaseRots[i] * offset;
            }
        }
    }

    // turns a colour and a global slot number into the array that owns that
    // laser and its index inside that array. pillars can have different laser
    // counts, so a slot number alone is not enough.
    public bool ResolveSlot(pillarColor color, int slot, out LaserArray owner, out int localIndex)
    {
        owner = null;
        localIndex = -1;

        if (slot < 0)
            return false;

        LaserArray[] arrays = getPillar(color);

        if (arrays == null) return false;

        int walked = 0;

        for (int i = 0 ; i < arrays.Length ; i++) {
            if (arrays[i] == null) continue;

            int count = arrays[i].Count;

            if (slot < walked + count) {
                owner = arrays[i];
                localIndex = slot - walked;
                return true;
            }

            walked += count;
        }
        return false;
    }

    // walks the arrays on a pillar to find which one owns a global slot
    private LaserArray getLaser(int pillar, int slot)
    {
        if (pillar < 0 || pillar >= pillars.Length)
            return null;

        LaserArray[] pillarArray = pillars[pillar];

        if (pillarArray == null)
            return null;
        if (slot < 0 || slot >= pillarArray.Length)
            return null;

        return pillarArray[slot];
    }

    // turns a color into its array, null if that pillar was never wired up
    private LaserArray[] getPillar(pillarColor color) {
        int colIndex = getPillarIndex(color);

        if (colIndex < 0 || colIndex >= pillars.Length)
            return null;

        return pillars[colIndex];

    }

    // turns a color into its index, keeps the mapping in one place
    private int getPillarIndex(pillarColor color) {
        switch (color) {
            case pillarColor.Red:
                return 0;
            case pillarColor.Blue:
                return 1;
            case pillarColor.Green:
                return 2;
            case pillarColor.Yellow:
                return 3;
        }

        return -1;
    }

    // total lasers on a pillar, summed across its arrays
    public int LaserCount(pillarColor color)
    {

        LaserArray[] arrays = getPillar(color);

        if (arrays == null) return 0;

        int total = 0;

        for (int i = 0 ; i < arrays.Length ; i++) {
            if (arrays[i] != null) {
                total += arrays[i].Count;
            }
        }

        return total;

    }

    // fires one specific laser, silently does nothing if the slot is bad
    public void FireLaser(pillarColor color , int slot) {
        if (!ResolveSlot(color , slot , out LaserArray owner , out int localIndex)) {
            return;
        }

        owner.MoveOne(localIndex , true);
    }

    // Stops one specific laser from firing, silently does nothing if the slot is bad
    public void StopLaser(pillarColor color , int slot) {
        if (!ResolveSlot(color , slot , out LaserArray owner , out int localIndex)) {
            return;
        }

        owner.MoveOne(localIndex , false);
    }

    // fires every laser on the pillar at once
    // loops off the actual array length so adding a laser to a pillar just works
    public void FirePillar(pillarColor color) {
        LaserArray[] arrays = getPillar(color);

        if (arrays == null) {
            return;
        }

        for (int i = 0 ; i < arrays.Length ; i++) {
            if (arrays[i] != null) {
                arrays[i].Deploy();
            }
        }
    }

    // retracts every laser on one pillar
    public void StopPillar(pillarColor color) {
        LaserArray[] arrays = getPillar(color);

        if (arrays == null) {
            return;
        }

        for (int i = 0 ; i < arrays.Length ; i++) {
            if (arrays[i] != null) {
                arrays[i].Retract();
            }
        }
    }

    // retracts everything on all four pillars
    // this is the cleanup call, phase transitions should always hit it
    public void StopAllLasers() {

        if (pillars == null)
            return;

        for (int p = 0 ; p < pillars.Length ; p++) {
            if (pillars[p] == null) { continue; }

            for (int i = 0 ; i < pillars[p].Length ; i++) {
                if (pillars[p][i] != null) {
                    pillars[p][i].RetractNow();
                }
            }
        }
    }

    // true if anything on this pillar is currently out and firing
    public bool IsFiring(pillarColor color)
    {
        LaserArray[] arrays = getPillar(color);
        if (arrays == null) { return false; }

        for (int i = 0 ; i < arrays.Length ; i++) {
            if (arrays[i] != null && arrays[i].IsAnyDeployed) {
                return true;
            }
        }

        return false;
    }

    // starts the group spinning forever, direction is 1 or -1
    // kills whatever rotation was already running first
    public void StartSpin(int direction) {
        
        if (spinTargets == null || spinTargets.Length == 0) {
            Debug.LogError("LaserArrayManager: no laserArrays to spin" , this);
            return;
        }

        StopRotation();

        rotateRoutine = StartCoroutine(spinLoop(direction));

    }

    // continuous rotation until something stops it. shares rotateRoutine with
    // sweep, so starting one cancels the other for free.
    private IEnumerator spinLoop(int direction)
    {
        int dir = direction < 0 ? -1 : 1;

        while (true) {

            float step = Mathf.Min(Time.unscaledDeltaTime , 0.05f);

            spinAngle += spinSpeed * dir * step;

            if (spinAngle >= 360f) {
                spinAngle -= 360f;
            }else if(spinAngle < -360f) {
                spinAngle += 360f;
            }

            applySpinAngle();

            yield return null;
        }
    }

    // rotates the group to a set angle and stops there
    // shares the rotate handle with the spin so calling this cancels a spin for free
    public void SweepTo(float angle) {
        
        if (spinTargets == null || spinTargets.Length == 0) {
            Debug.LogError("LaserArrayManager: no laserArrays to spin" , this);
            return;
        }

        StopRotation();
        rotateRoutine = StartCoroutine(sweepRoutine(angle));
    }

    // rotates to a set angle and stops. picks the shorter way round, overshoots
    // slightly, then snaps exact so it never creeps.
    private IEnumerator sweepRoutine(float target)
    {

        // a speed of 0 never closes the gap and the loop would run for the rest of the fight
        if (sweepSpeed <= 0f) {
            Debug.LogError("LaserArrayManager: sweepSpeed is 0, snapping to the angle instead" , this);
            spinAngle = target;
            applySpinAngle();
            rotateRoutine = null;
            yield break;
        }

        float remaining = Mathf.DeltaAngle(spinAngle , target);

        while (Mathf.Abs(remaining) > angleEpsilon) {
            // dont overshoot on the last frame, only move as far as whats left
            float step = sweepSpeed * Mathf.Min(Time.unscaledDeltaTime,0.05f);
            step = Mathf.Min(step , Mathf.Abs(remaining)) * Mathf.Sign(remaining);

            spinAngle += step;
            applySpinAngle();

            remaining = Mathf.DeltaAngle(spinAngle , target);

            yield return null;
        }

        // snap so we end on an exact angle, not epsilon short of one
        spinAngle = target;
        applySpinAngle();

        // clear our own handle, otherwise getIsSpinning reports true forever
        rotateRoutine = null;

    }

    // stops any rotation and leaves the pivot wherever it ended up
    public void StopRotation() {
        if (rotateRoutine == null)
            return;

        StopCoroutine(rotateRoutine);
        rotateRoutine = null;
    }

    // just a null check, the handle being alive means its rotating
    public bool IsSpinning => rotateRoutine != null;


    [ContextMenu("Test Spin Clockwise")]
    private void testSpinClockwise() {
        if (spinSpeed <= 0f) {
            Debug.LogError("LaserArrayManager:spinSpeed is 0" , this);
            return;
        }

        StartSpin(1);
        Debug.Log("LaserArrayManager: spinning clockwise at " + spinSpeed + " deg per sec" , this);
    }

    [ContextMenu("Test Spin Counter Clockwise")]
    private void testSpinCounterClockwise() {
        if (spinSpeed <= 0f) {
            Debug.LogError("LaserArrayManager: spinSpeed is 0" , this);
            return;
        }

        StartSpin(-1);
        Debug.Log("LaserArrayManager: spinning counter clockwise at " + spinSpeed + " deg per sec" , this);
    }

    [ContextMenu("Test Sweep to 90")]
    private void testSweep() {
        if (sweepSpeed <= 0f) {
            Debug.LogError("LaserArrayManager: sweepSpeed is 0, loop would never finish" , this);
            return;
        }

        Debug.Log("LaserArrayManager: sweeping from "+ spinAngle +" to 90",this);
        SweepTo(90);
    }

    [ContextMenu("Stop Rotation")]
    private void testStopRotation() {
        StopRotation();
        Debug.Log("LaserArrayManager: rotation stopped at " +  spinAngle , this);
    }

    [ContextMenu("Reset Pivot Rotation")]
    private void resetPivot() {
        StopRotation();
        spinAngle = 0f;
        applySpinAngle();

        Debug.Log("LaserArrayManager: pivot reset to 0" , this);
    }


    [ContextMenu("Check Rotation Setup")]
    // logs whether every array is where the spin expects it. inspector only,
    // for catching a pillar that was moved without updating its base rotation.
    private void checkRotationSetup()
    {
        int count = spinTargets == null ? 0 : spinTargets.Length;

        Debug.Log("LaserArrayManager rotation setup:"
            + "\n   arrays registered: " + count
            + "\n   spinSpeed: " + spinSpeed
            + "\n   sweepSpeed: " + sweepSpeed
            + "\n   currently rotating: " + IsSpinning
            + "\n   spinAngle: " + spinAngle.ToString("F1") , this);

        if (count == 0) {
            Debug.LogError("LaserArrayManager: no arrays registered, check the four pillar lists" , this);
            return;
        }

        // eight is what the arena has, anything less means a pillar list lost an entry in a merge
        if (count != 8)
            Debug.LogWarning("LaserArrayManager: expected 8 arrays, found " + count , this);

        if (spinSpeed <= 0f)
            Debug.LogError("LaserArrayManager: spinSpeed is 0, the arrays rotate by 0 every frame" , this);

        if (sweepSpeed <= 0f)
            Debug.LogError("LaserArrayManager: sweepSpeed is 0, sweepRoutine would loop forever and block every later rotation call" , this);

        // if an array isnt sitting where applySpinAngle put it, something else is writing
        // to that transform and fighting the spin
        Quaternion expected = Quaternion.Euler(0f , spinAngle , 0f);
        int drifted = 0;

        for (int i = 0 ; i < spinTargets.Length ; i++) {
            if (spinTargets[i] == null) {
                Debug.LogError("LaserArrayManager: array slot " + i + " is empty" , this);
                continue;
            }

            float off = Quaternion.Angle(spinTargets[i].transform.localRotation , spinBaseRots[i] * expected);

            if (off > 1f) {
                Debug.LogError("LaserArrayManager: '" + spinTargets[i].name + "' is " + off.ToString("F1")
                    + " degrees off where the spin put it, something else is moving it" , spinTargets[i]);
                drifted++;
            }
        }

        if (drifted == 0)
            Debug.Log("LaserArrayManager: all " + count + " arrays are where the spin expects them" , this);
    }

    // runs one of the authored patterns by index, cancels any pattern already going
    public void StartPattern(int patternIndex) {
        if (patterns == null)
            return;
        if (patternIndex < 0 || patternIndex >= patterns.Length)
            return;

        laserPattern chosen = patterns[patternIndex];

        if (chosen == null || chosen.steps == null || chosen.steps.Length == 0)
            return;

        StopPattern();
        patternRoutine = StartCoroutine(patternLoop(chosen));
    }

    // walks an authored pattern step by step until told to stop
    private IEnumerator patternLoop(laserPattern pattern)
    {
        do {
            for (int i = 0 ; i < pattern.steps.Length ; i++) {
                laserStep step = pattern.steps[i];

                runStep(step);

                // waitForSeconds(0) still burns a frame, skipping the yield is what makes
                // a delay of 0 actually fire together with the next step
                if (step.delay > 0f) {
                    yield return new WaitForSeconds(step.delay);
                }
            }

            // safety yield, a looping pattern with every delay at 0 would hang unity without this
            if (pattern.loops) {
                yield return null;
            }
        }
        while (pattern.loops);

        // clear our own handle so getIsPatternRunning goes false when we finish naturally
        patternRoutine = null;
    }

    // kills the pattern and retracts everything
    // the retract half is the part thats easy to forget, without it lasers stay on after the phase ends
    public void StopPattern() {
        if (patternRoutine != null) {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }

        StopAllLasers();
    }

    // fires or retracts whatever one step asks for, whole pillar or single laser
    private void runStep(laserStep step)
    {
        if (step.slot < 0) {
            if (step.retract) {
                StopPillar(step.color);
            } else {
                FirePillar(step.color);
            }
        } else {
            if (step.retract) {
                StopLaser(step.color,step.slot);
            }else {
                FireLaser(step.color , step.slot);
            }
        }
            
    }

    public void StartGeneratedPattern(float difficulty) {
        StopPattern();
        patternRoutine = StartCoroutine(generatedLoop(Mathf.Clamp01(difficulty)));
    }


    // builds and runs a fresh sequence instead of an authored one. difficulty
    // lerps every generator setting between its easy and hard value.
    private IEnumerator generatedLoop(float difficulty)
    {

        while (true) {
            float[] weights = rollPillarWeights(difficulty);

            Vector2 stepRange = Vector2.Lerp(generator.stepsEasy , generator.stepsHard , difficulty);
            int minSteps = Mathf.Max(1 , Mathf.RoundToInt(stepRange.x));
            int maxSteps = Mathf.Max(minSteps , Mathf.RoundToInt(stepRange.y));
            int stepCount = Random.Range(minSteps , maxSteps + 1);

            Vector2 delayRange = Vector2.Lerp(generator.delayEasy, generator.delayHard , difficulty);
            float minDelay = Mathf.Max(0f , jittered(delayRange.x));
            float maxDelay = Mathf.Max(minDelay, jittered(delayRange.y));

            float retractChance = Mathf.Clamp01(jittered(Mathf.Lerp(generator.retractEasy, generator.retractHard , difficulty)));
            float wholePillarChance = Mathf.Clamp01(jittered(Mathf.Lerp(generator.wholePillarEasy, generator.wholePillarHard , difficulty)));

            AnimationCurve slotBias = rollSlotBias();

            for (int i = 0 ; i < stepCount ; i++) {
                bool wantRetract = Random.Range(0f , 1f) < retractChance && anyDeployed();

                laserStep step = new laserStep();

                step.color = rollColor(weights , wantRetract);
                step.retract = wantRetract;
                step.slot = Random.Range(0f , 1f) < wholePillarChance ? -1 : rollSlot(step.color , slotBias , wantRetract);

                if (step.slot < -1)
                    continue;

                runStep(step);

                float wait =  Random.Range(minDelay, maxDelay);

                if (wait > 0f) {
                    yield return new WaitForSecondsRealtime(wait);
                }
                    
            }
            yield return null;
        }
    }

    // decides how likely each pillar is to be picked. more pillars come into
    // play as difficulty rises.
    private float[] rollPillarWeights(float difficulty)
    {
        float[] weights = new float[4];

        int wanted = Mathf.RoundToInt(Mathf.Lerp(generator.pillarsEasy,generator.pillarsHard,difficulty));

        int[] order = new int[4];

        for (int i = 0 ; i < 4 ; i++) {
            order[i] = i;
        }

        for (int i = order.Length-1 ;i>0 ; i--) {
            int swap = Random.Range(0 , i + 1);
            int temp = order[i];
            order[i] = order[swap];
            order[swap] = temp;
        }

        int taken = 0;

        for (int i = 0 ;i<order.Length ;i++) {
            if (taken >= wanted) {
                break;
            }

            if (LaserCount((pillarColor)order[i]) == 0){
                continue;
            }

            weights[order[i]] = 1f;
            taken++;
        }

        if (taken == 0) {
            for (int i = 0 ; i < 4 ; i++) {
                weights[i] = 1f;
            }
        }

        return weights;
    }


    // picks one of the slot bias curves, so two passes at the same difficulty
    // favour different heights
    private AnimationCurve rollSlotBias()
    {
        if (generator.slotBiasOptions == null || generator.slotBiasOptions.Length == 0) {
            return AnimationCurve.Linear(0f , 1f , 1f , 1f);
        }

        AnimationCurve picked = generator.slotBiasOptions[Random.Range(0,generator.slotBiasOptions.Length)];

        return picked != null ? picked : AnimationCurve.Linear(0f , 1f , 1f , 1f);
    }

    // nudges a value randomly within the jitter range, so nothing is exactly
    // repeatable at a given difficulty
    private float jittered(float value)
    {
        if (generator.jitter <= 0) {
            return value;
        }

        return value * (1f + Random.Range(-generator.jitter , generator.jitter));
    }


    // picks a pillar by weight. needsDeployed limits it to pillars that already
    // have something out, used when the step is a retract.
    private pillarColor rollColor(float[] weights, bool needsDeployed)
    {


        float[] live = new float[4];
        float total = 0f;

        for (int i = 0 ; i < 4 ; i++) {
            pillarColor color = (pillarColor)i;

            live[i] = weights[i];

            if (LaserCount(color) == 0) {
                live[i] = 0f;
            } else if (needsDeployed && !hasDeployed(color)) {
                live[i] = 0f;
            }

            total += live[i];
        }

        if (total <= 0f) {
            for (int i = 0 ;i < 4 ;i++) {
                if (LaserCount((pillarColor)i) > 0) {
                    return (pillarColor)i;
                }
            }

            return pillarColor.Red;
        }

        float roll = Random.Range(0f , total);

        for (int i = 0 ;i<4 ; i++) {
            if (roll < live[i]) {
                return (pillarColor)i;
            }
            roll -= live[i];
        }

        return pillarColor.Red;
    }


    // picks a slot on a pillar, biased by the curve so patterns favour certain heights
    private int rollSlot(pillarColor color, AnimationCurve slotBias, bool needsDeployed)
    {
        int count = LaserCount(color);

        if (count <= 0) {
            return -2;
        }

        float total = 0f;
        float[] weights = new float[count];

        for (int i = 0 ; i < count ; i++) {
            if (!ResolveSlot(color,i,out LaserArray owner,out int localIndex)) {
                weights[i] = 0f;
                continue;
            }

            if (needsDeployed != owner.IsLaserOut(localIndex)) {
                weights[i] = 0f;
                continue;
            }

            float height = count == 1 ? 0f : (float)i / (count - 1);
            weights[i] = Mathf.Max(0f , slotBias.Evaluate(height));

            total += weights[i];
        }

        if (total<= 0f) {
            return -2;
        }

        float roll = Random.Range(0f , total);

        for (int i = 0 ; i<count ; i++) {
            if (roll < weights[i]) {
                return i;
            }

            roll -= weights[i];
        }

        return -2;
    }

    // true if this pillar has at least one laser out
    private bool hasDeployed(pillarColor color)
    {
        int count = LaserCount(color);

        for (int i = 0 ; i < count ; i++) {
            if (ResolveSlot(color,i,out LaserArray owner,out int localIndex) && owner.IsLaserOut(localIndex)) {
                return true;
            }
        }

        return false;
    }

    // true if anything anywhere is out, so a retract step has something to retract
    private bool anyDeployed()
    {
        for (int i = 0 ; i < 4; i++) {
            if (hasDeployed((pillarColor)i)) {
                return true;
            }
        }

        return false;
    }


    // null check on the handle
    public bool IsPatternRunning => patternRoutine != null;
    
}
