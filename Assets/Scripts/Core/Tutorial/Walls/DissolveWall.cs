using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/*
 * Script: DissolveWall
 *
 * Description:
 * One dissolving wall face. Tracks progress from 0 to 1 on scaled time, so the
 * hole only really grows while the player moves. Owns the timing, the blocking
 * collider and the events; DissolveWallVisuals or DissolveCubePieces draw it.
 *
 * Interacts With:
 * - RingBoundary (StartDissolve, FinishNow, ResetWall, Dissolved)
 * - DissolveWallVisuals, DissolveCubePieces (ProgressChanged)
 *
 * Notes:
 * - Time scale comes from stress, not speed. Standing still sits at TimeManager's
 *   0.1 floor and walking gets to about 0.6, so the default 4 scaled seconds is
 *   roughly 8 real seconds of walking and 40 standing still.
 */
public class DissolveWall : MonoBehaviour
{

    [Header("Timing")]
    [Tooltip("scaled seconds to fully dissolve, about 8 real seconds of walking at the default")]
    [SerializeField] private float dissolveSeconds = 4f;

    [Tooltip("share of normal speed that runs on real time on top, TimeManager's 0.1 floor already keeps the wall creeping so leave at 0")]
    [Range(0f, 1f)]
    [SerializeField] private float restCrawl = 0f;

    [Tooltip("square roots the progress so the hole's area grows steadily instead of rushing at the end")]
    [SerializeField] private bool isAreaSteady = true;

    [Tooltip("starts as soon as the scene loads, only for testing one face by itself")]
    [SerializeField] private bool shouldDissolveOnStart = false;


    [Header("Shape")]
    [Tooltip("where the hole starts, at eye height (about 1.6 m) and halfway through the 0.5 m thickness, with its x axis along the wall")]
    [SerializeField] private Transform dissolveCenter;

    [Tooltip("distance from the center to the farthest corner, the hole's radius at full progress, 3 on a straight 5 by 3 wall, about 3.9 on the 7.07 m diagonal")]
    [SerializeField] private float maxRadiusMeters = 3f;

    [Header("Blocking")]
    [Tooltip("collider that keeps the player in until the face is gone")]
    [SerializeField] private Collider blockingCollider;

    [Header("Events")]
    [Tooltip("fires every frame of the dissolve with the shaped progress, 0 to 1")]
    public UnityEvent<float> ProgressChanged = new UnityEvent<float>();

    [Tooltip("fires once when the face is gone, hook the mesh's SetActive(false) here until the dissolve shader exists")]
    public UnityEvent Dissolved = new UnityEvent();

    // raw progress, 0 is a full wall and 1 is gone
    private float progress = 0f;

    // stops a double finish
    private bool isDissolved = false;

    // single guarded dissolve routine
    private Coroutine dissolveRoutine;

    // raw progress, read only
    public float Progress => progress;

    // progress after the steady area shaping, what visuals should use
    public float ShapedProgress => shapeProgress(progress);

    // current hole radius in meters
    public float CurrentRadius => ShapedProgress * maxRadiusMeters;

    // world position of the hole's center, falls back to the wall itself if no center is set
    public Vector3 CenterPosition => dissolveCenter != null ? dissolveCenter.position : transform.position;

    // the full radius, for visuals and cube sorting
    public float MaxRadius => maxRadiusMeters;

    // runs along the wall, the visuals place edge bursts with it
    public Vector3 AlongWall => dissolveCenter != null ? dissolveCenter.right : transform.right;

    public bool IsDissolved => isDissolved;

    private void OnDisable()
    {
        stopRoutine();
    }

    private void Start()
    {
        if (shouldDissolveOnStart)
        {
            StartDissolve();
        }
    }

    // RingBoundary calls this
    public void StartDissolve()
    {
        if (dissolveRoutine != null || isDissolved)
        {
            return;
        }

        // a coroutine can't start on an inactive object
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("DissolveWall: can't dissolve while inactive", this);
            return;
        }

        // a zero duration would divide by zero, so it just finishes
        if (dissolveSeconds <= 0f)
        {
            FinishNow();
            return;
        }

        dissolveRoutine = StartCoroutine(DissolveOverTime());
    }

    // debug skip, gone this frame
    public void FinishNow()
    {
        if (isDissolved)
        {
            return;
        }

        stopRoutine();
        progress = 1f;
        ProgressChanged.Invoke(ShapedProgress);
        finishDissolve();
    }



    // back to a full wall, for space restarts and testing
    public void ResetWall()
    {
        stopRoutine();
        progress = 0f;
        isDissolved = false;

        if (blockingCollider != null)
        {
            blockingCollider.enabled = true;
        }

        ProgressChanged.Invoke(0f);
    }

    private IEnumerator DissolveOverTime()
    {
        while (progress < 1f)
        {
            // scaled time is the lesson, the rest crawl runs on real time no matter what
            progress += (Time.deltaTime + restCrawl * Time.unscaledDeltaTime) / dissolveSeconds;
            progress = Mathf.Clamp01(progress);
            ProgressChanged.Invoke(ShapedProgress);
            yield return null;
        }

        dissolveRoutine = null;
        finishDissolve();
    }

    // turns raw progress into what visuals use
    private float shapeProgress(float raw)
    {
        // the hole's area grows with radius squared, so the square root keeps the area steady
        return isAreaSteady ? Mathf.Sqrt(raw) : raw;
    }

    // the face is gone, let the player through
    private void finishDissolve()
    {
        isDissolved = true;

        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }

        Dissolved.Invoke();
    }

    private void stopRoutine()
    {
        if (dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }
    }
}
