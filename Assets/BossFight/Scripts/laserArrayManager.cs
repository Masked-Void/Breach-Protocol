using System;
using System.Collections;
using UnityEngine;

public enum pillarColor { Red, Blue, Green, Yellow };

// one step in a firing sequence
[System.Serializable]
public struct laserStep {
    [Tooltip("which pillar this step hits")]
    public pillarColor color;

    // -1 is the sentinel for the whole pillar, otherwise its an index into that pillars array
    [Tooltip("which laser on the pillar. use -1 to fire the entire pillar at once")]
    public int slot;

    [Tooltip("seconds to wait after this step. set it to 0 to fire together with the next one")]
    public float delay;
}

// a full sequence of steps
[System.Serializable]
public class laserPattern {
    [Tooltip("just a label for the inspector, never read at runtime")]
    public string patternName;

    [Tooltip("the sequence, runs top to bottom")]
    public laserStep[] steps;

    [Tooltip("repeat forever. looping patterns only end when stopPattern gets called")]
    public bool loops;
}



public class laserArrayManager : MonoBehaviour {

    [Header("Pillar Wiring")]
    [Tooltip("lasers on the Red pillar, ordered bottom to top. size is per pillar, they dont have to match")]
    [SerializeField] private laserArray[] pillarRed;

    [Tooltip("lasers on the Blue pillar, ordered bottom to top")]
    [SerializeField] private laserArray[] pillarBlue;

    [Tooltip("lasers on the Green pillar, ordered bottom to top")]
    [SerializeField] private laserArray[] pillarGreen;

    [Tooltip("lasers on the Yellow pillar, ordered bottom to top")]
    [SerializeField] private laserArray[] pillarYellow;

    [Header("Rotation")]
    [Tooltip("parent object all four pillars sit under. rotation goes through this, never the lasers themselves")]
    [SerializeField] private Transform arrayPivot;

    [Tooltip("degrees per second for the continuous spin. direction comes from the startSpin call, not from here")]
    [SerializeField] private float spinSpeed = 45f;

    [Tooltip("degrees per second when sweeping to a set angle. usually faster than the ambient spin")]
    [SerializeField] private float sweepSpeed = 120f;

    [Tooltip("how close to the target angle counts as arrived. keep it above one frame of travel or it overshoots forever")]
    [SerializeField] private float angleEpsilon = 0.5f;

    [Header("Patterns")]
    [Tooltip("all the firing sequences. the boss controller calls these by index")]
    [SerializeField] private laserPattern[] patterns;

    // built in awake from the four fields above. unity wont serialize a jagged array
    // but nothing stops us making one at runtime, thats the whole reason the fields are split
    private laserArray[][] pillars;

    // two handles so the group can spin while a pattern is firing
    private Coroutine rotateRoutine;
    private Coroutine patternRoutine;



    // stitch the four inspector fields into something indexable
    // has to match the pillarColor enum order or the colors get swapped
    private void Awake() {
        pillars = new laserArray[4][];

        pillars[0] = pillarRed;
        pillars[1] = pillarBlue;
        pillars[2] = pillarGreen;
        pillars[3] = pillarYellow;
    }

    // single point of validation for every fire call
    // checks the pillar index, the slot index, and whether the entry is actually filled in
    // returns null on any miss so a bad pattern step just does nothing instead of throwing mid fight
    private laserArray getLaser(int pillar , int slot) {
        if (pillar < 0 || pillar >= pillars.Length)
            return null;

        laserArray[] pillarArray = pillars[pillar];

        if (pillarArray == null)
            return null;
        if (slot < 0 || slot >= pillarArray.Length)
            return null;

        return pillarArray[slot];
    }

    // turns a color into its array, null if that pillar was never wired up
    private laserArray[] getPillar(pillarColor color) {
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

    // how many lasers this pillar actually has, 0 if its empty
    // nothing should ever hardcode a count, ask here instead
    public int getLaserCount(pillarColor color) {
        laserArray[] pillarArray = getPillar(color);

        if (pillarArray == null)
            return 0;

        return pillarArray.Length;
    }

    // fires one specific laser, silently does nothing if the slot is bad
    public void fireLaser(pillarColor color , int slot) {
        laserArray target = getLaser(getPillarIndex(color) , slot);

        if (target == null)
            return;

        target.deploy();
    }

    // Stops one specific laser from firing, silently does nothing if the slot is bad
    public void stopLaser(pillarColor color , int slot) {
        laserArray target = getLaser(getPillarIndex(color) , slot);

        if (target == null)
            return;

        target.retract();
    }

    // fires every laser on the pillar at once
    // loops off the actual array length so adding a laser to a pillar just works
    public void firePillar(pillarColor color) {
        int count = getLaserCount(color);

        for (int i = 0 ; i < count ; i++) {
            fireLaser(color , i);
        }
    }

    // retracts every laser on one pillar
    public void stopPillar(pillarColor color) {
        int count = getLaserCount(color);

        for (int i = 0 ; i < count ; i++) {
            stopLaser(color , i);
        }
    }

    // retracts everything on all four pillars
    // this is the cleanup call, phase transitions should always hit it
    public void stopAllLasers() {
        int pilCount = pillars.Length;

        var colorVals = (pillarColor[])Enum.GetValues(typeof(pillarColor));

        for (int i = 0 ; i < pilCount ; i++) {
            stopPillar(colorVals[i]);
        }
    }

    // true if any laser on this pillar is currently deployed
    // needs a matching accessor over on laserArray
    public bool getIsFiring(pillarColor color) {
        int count = getLaserCount(color);

        for (int i = 0 ; i < count ; i++) {
            laserArray target = getLaser(getPillarIndex(color) , i);

            if (target == null)
                continue;

            if (target.getIsDeployed)
                return true;
        }

        return false;
    }

    // starts the group spinning forever, direction is 1 or -1
    // kills whatever rotation was already running first
    public void startSpin(int direction) {

        if (arrayPivot == null)
            return;

        stopRotation();

        rotateRoutine = StartCoroutine(spinLoop(direction));

    }

    // spins the pivot every frame and never exits on its own
    // only stopRotation or a sweepTo call can end this
    private IEnumerator spinLoop(int direction) {
        int dir = direction < 0 ? -1 : 1;

        while (true) {
            arrayPivot.Rotate(0f , spinSpeed * dir * Time.deltaTime , 0f);
            yield return null;
        }
    }

    // rotates the group to a set angle and stops there
    // shares the rotate handle with the spin so calling this cancels a spin for free
    public void sweepTo(float angle) {
        if (arrayPivot == null)
            return;

        stopRotation();
        rotateRoutine = StartCoroutine(sweepRoutine(angle));
    }

    // normalize the current angle first or the accumulated spin makes the math wrong
    // then pick whichever direction is shorter, rotate until past the target, snap exact
    private IEnumerator sweepRoutine(float target) {
        float remaining = Mathf.DeltaAngle(arrayPivot.eulerAngles.y , target);

        while (Mathf.Abs(remaining) > angleEpsilon) {
            // dont overshoot on the last frame, only move as far as whats left
            float step = sweepSpeed * Time.deltaTime;
            step = Mathf.Min(step , Mathf.Abs(remaining)) * Mathf.Sign(remaining);

            arrayPivot.Rotate(0f , step , 0f);

            // recalculate off the actual transform instead of subtracting, keeps it honest
            remaining = Mathf.DeltaAngle(arrayPivot.eulerAngles.y , target);

            yield return null;
        }

        // snap so we end on an exact angle, not epsilon short of one
        Vector3 finalAngles = arrayPivot.eulerAngles;
        finalAngles.y = target;
        arrayPivot.eulerAngles = finalAngles;

        // clear our own handle, otherwise getIsSpinning reports true forever
        rotateRoutine = null;

    }

    // stops any rotation and leaves the pivot wherever it ended up
    public void stopRotation() {
        if (rotateRoutine == null)
            return;

        StopCoroutine(rotateRoutine);
        rotateRoutine = null;
    }

    // just a null check, the handle being alive means its rotating
    public bool getIsSpinning() {
        return rotateRoutine != null;
    }

    // runs one of the authored patterns by index, cancels any pattern already going
    public void startPattern(int patternIndex) {
        if (patterns == null)
            return;
        if (patternIndex < 0 || patternIndex > patterns.Length)
            return;

        laserPattern chosen = patterns[patternIndex];

        if (chosen == null || chosen.steps == null || chosen.steps.Length == 0)
            return;

        stopPattern();
        patternRoutine = StartCoroutine(patternLoop(chosen));
    }

    // walks the steps, slot -1 means fire the whole pillar, otherwise fire the one laser
    // waits the steps delay after firing so a delay of 0 fires with the next step
    // loops back to the start if the pattern is set to loop, otherwise exits and clears the handle
    private IEnumerator patternLoop(laserPattern pattern) {
        do {
            for (int i = 0 ; i < pattern.steps.Length ; i++) {
                laserStep step = pattern.steps[i];

                if (step.slot < 0) {
                    firePillar(step.color);
                } else {
                    fireLaser(step.color , step.slot);
                }

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
    public void stopPattern() {
        if (patternRoutine != null) {
            StopCoroutine(patternRoutine);
            patternRoutine = null;
        }

        stopAllLasers();
    }

    // null check on the handle
    public bool getIsPatternRunning() {
        return patternRoutine != null;
    }
}
