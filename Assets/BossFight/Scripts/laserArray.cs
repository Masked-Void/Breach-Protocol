using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class laserArray : MonoBehaviour
{

    [Header("Marker Names: (Children of each laser)")]
    [SerializeField] string laserInMarkerName = "laserIn";
    [SerializeField] string laserOutMarkerName = "laserOut";

    [Header("Deploy Motion")]
    [SerializeField] float deployTime = 1f;
    [SerializeField] float stagger = 0.5f;

    [ContextMenu("Deploy")]
    public void deploy()
    {
        moveAll(true);
    }

    [ContextMenu("Retract")]
    public void retract()
    {
        moveAll(false);
    }

    class laserUnit
    {
        public Transform laser;
        public Transform laserIn;
        public Transform laserOut;
        public Collider[] beams;
        public float progress;
        public Coroutine moveRoutine;
    }

    laserUnit[] lasers;

    Coroutine groupRoutine;

    bool isOut = false;

    public bool IsOut
    {
        get
        {
            return isOut;
        }
    }

    private void Awake()
    {
        build();
    }

    void build()
    {

        int count = transform.childCount;
        lasers = new laserUnit[count];

        Transform[] children = new Transform[count];

        for (int i = 0; i < count; i++) { 
        
            children[i] = transform.GetChild(i);
        
        }

        for (int i = 0; i < count; i++)
        {
            Transform laser = children[i];

            laserUnit laserUnit = new laserUnit();

            laserUnit.laser = laser;
            laserUnit.laserIn = findMark(laser, laserInMarkerName);
            laserUnit.laserOut = findMark(laser, laserOutMarkerName);
            laserUnit.progress = 0f;

            if (laserUnit.laserIn == null || laserUnit.laserOut == null)
            {
                Debug.LogError("laserArray: '" + laser.name + "' needs two children named '"
                + laserInMarkerName + "' and '" + laserOutMarkerName + "'.", laser);
                enabled = false;
                return; 
            }

            laserUnit.beams = laser.GetComponentsInChildren<Collider>(true);

            laserUnit.laserIn.SetParent(transform, true);
            laserUnit.laserOut.SetParent(transform, true);

            lasers[i] = laserUnit;
            setBeam(laserUnit, false);
        }
    }

    Transform findMark(Transform laser, string wanted)
    {
        foreach (Transform child in laser)
        {
            if (child.name == wanted)
            {
                return child;
            }
        }

        return null;
    }

    private void LateUpdate()
    {
        if (!enabled)
        {
            return;
        }

        for (int i = 0; i < lasers.Length; i++)
        {
            placeLaser(lasers[i]);
        }
    }

    public void moveOne(int index,bool goOut)
    {
        if (!enabled)
        {
            return;
        }

        startMove(lasers[index],goOut);
    }

    void moveAll(bool goOut)
    {
        if (!enabled)
        {
            return;
        }

        isOut = goOut;

        if (groupRoutine != null)
        {
            StopCoroutine(groupRoutine);
        }

        groupRoutine = StartCoroutine(moveGroup(goOut));
    }

    IEnumerator moveGroup(bool goOut)
    {
        for (int i = 0; i < lasers.Length; i++)
        {
            startMove(lasers[i], goOut);

            if (stagger > 0)
            {
                yield return new WaitForSeconds(stagger);
            }
        }

        groupRoutine = null;
    }

    void startMove(laserUnit unit, bool goOut)
    {
        if (unit.moveRoutine != null)
        {
            StopCoroutine(unit.moveRoutine);
        }

        float target = goOut ? 1f : 0f;
        unit.moveRoutine = StartCoroutine(moveLaser(unit, target));
    }

    IEnumerator moveLaser(laserUnit unit, float target)
    {
        setBeam(unit, false);

        float start = unit.progress;

        float distance = Mathf.Abs(target - start);

        float duration = deployTime * distance;

        float timePassed = 0f;

        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;

            float howFar = Mathf.Clamp01(timePassed / duration);

            unit.progress = Mathf.Lerp(start, target, howFar);
            placeLaser(unit);

            yield return null;
        }

        unit.progress = target;
        placeLaser(unit);

        if (target >= 1f) {
            setBeam(unit, true);
        }

        unit.moveRoutine = null;
    }

    void placeLaser(laserUnit unit)
    {
        unit.laser.position = Vector3.Lerp(unit.laserIn.position, unit.laserOut.position, unit.progress);
        unit.laser.rotation = Quaternion.Slerp(unit.laserIn.rotation, unit.laserOut.rotation, unit.progress);
    }

    void setBeam(laserUnit unit, bool on)
    {
        for (int i = 0; i < unit.beams.Length; i++)
        {
            if (unit.beams[i] != null)
                unit.beams[i].enabled = on;
        }
    }
}
