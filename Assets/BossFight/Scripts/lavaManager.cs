using UnityEngine;
using System.Collections;
using UnityEngine.Assemblies;
using Unity.VisualScripting;

public class lavaManager : MonoBehaviour
{

    [Header("Drag in Lava")]
    [SerializeField] Transform lavaObject;

    [Header("Marker names (children of the lava object)")]
    [SerializeField] string lowMarkerName = "Low";
    [SerializeField] string highMarkerName = "High";

    [Header("Motion (Uses unscaledDeltaTime)")]
    [Tooltip("Seconds for a full drained-to-full rise.")]
    [SerializeField] float riseTime = 25f;
    [Tooltip("Seconds for a full drain.")]
    [SerializeField] float drainTime = 4f;
    
    private Transform lowPos;
    private Transform highPos;

    private float progress = 0f;

    private Coroutine routine;

    [ContextMenu("Rise")]
    public void rise()
    {
        moveTo(1f);
    }

    [ContextMenu("Drain")]
    public void drain()
    {
        moveTo(0f);
    }

    [ContextMenu("Reset To Drained")]
    public void resetToDrained()
    {
        setNow(0f);
    }

    public float Level
    {
        get
        {
            return progress;
        }
    }

    public float SurfaceY
    {
        get
        {
            return lavaObject.position.y;
        }
    }
    void Awake()
    {
        if (lavaObject == null)
        {
            Debug.LogError("lavaManager: lavaObject isn't assigned.", this);
            enabled = false;
            return;
        }

        lowPos = findMark(lowMarkerName);
        highPos = findMark(highMarkerName);

        if (lowPos == null|| highPos == null)
        {
            Debug.LogError("lavaManager: '" + lavaObject.name + "' needs two children named '"
                + lowMarkerName + "' and '" + highMarkerName + "'.", lavaObject);
            enabled = false;
            return;
        }

        lowPos.SetParent(transform, true);
        highPos.SetParent(transform, true);

        placeLava();
    }

  
    Transform findMark(string wanted)
    {
        foreach (Transform child in lavaObject)
        {
            if (child.name == wanted)
            {
                return child;
            }
        }

        return null;
    }

    public void moveTo(float amt)
    {

        if (!enabled) return;

        amt = Mathf.Clamp01(amt);

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(moveLava(amt));

    }

    public void setNow(float amt)
    {
        
        if (!enabled) return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        progress = Mathf.Clamp01(amt);
        placeLava();
    
    }

    IEnumerator moveLava(float target)
    {
        
        float start = progress;

        float distance = Mathf.Abs(target - start);

        float fullTripTime = (target > start) ? riseTime : drainTime;

        float duration = fullTripTime * distance;

        float timePassed = 0f;

        while (timePassed < duration)
        {
            float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            timePassed += step;
            float howFar = Mathf.Clamp01(timePassed / duration);

            progress = Mathf.Lerp(start, target, howFar);
            placeLava();

            yield return null;
        }

        progress = target;
        placeLava();

        routine = null;
    }

    void placeLava()
    {
        lavaObject.position = Vector3.Lerp(lowPos.position, highPos.position, progress);
    }
   
}