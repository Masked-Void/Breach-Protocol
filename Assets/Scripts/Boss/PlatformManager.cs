using System.Collections;
using UnityEngine;

/*
 * Script: PlatformManager
 *
 * Description:
 * The rising and falling platforms in the boss arena. Platforms are grouped
 * into stages, and each stage fires on a delay after the last so the arena
 * changes shape in a wave rather than all at once.
 *
 * Responsibilities:
 * - Cache each platform's start position and travel height on Awake
 * - Raise and lower every stage on a stagger
 * - Keep one pass running at a time so a rise and fall never fight
 *
 * Interacts With:
 * - TrapManager (calls RisePlatforms and FallPlatforms per phase)
 *
 * Notes:
 * - Runs on unscaled time like every boss hazard.
 * - One passRoutine handle means calling Rise mid-Fall cancels the fall
 *   cleanly instead of both running.
 */
public class PlatformManager : MonoBehaviour
{

    [System.Serializable]
    public class platformStage
    {
        [Tooltip("platforms that belong to this stage")]
        public Transform[] platforms;

        [Tooltip("marker that sets the top height for this stage")]
        public Transform topMarker;

        [Tooltip("how fast these platforms rise, 1 means full travel in one second")]
        public float riseSpeed = 1f;

        [Tooltip("how fast these platforms fall, 1 means full travel in one second")]
        public float fallSpeed = 1f;

        // filled in at runtime
        [HideInInspector] public Vector3[] startPositions;
        [HideInInspector] public float[] current;
        [HideInInspector] public int[] order;
        [HideInInspector] public Coroutine[] platformRoutines;
        [HideInInspector] public float riseHeight;
        [HideInInspector] public bool ready;
    }

    [Header("Stages")]

    [Tooltip("every stage in order, add or remove stages here instead of making new fields")]
    [SerializeField] platformStage[] stages;

    [Tooltip("marker that sets the bottom height every stage returns to")]
    [SerializeField] Transform stageBase;

    [Header("Stagger Settings")]

    [Tooltip("delay between the start of each stage")]
    [SerializeField] float stageStagger = 0.5f;

    [Tooltip("delay between platforms inside a stage, set to 0 to move the whole stage at once")]
    [SerializeField] float inStageStagger = 0.1f;

    // single handle so a rise and a fall can never run at the same time
    private Coroutine passRoutine;

    private Coroutine[] stageRoutines;


    void Awake()
    {
        cacheStages();
    }


    // reads each stage's markers and stores start positions and travel distance.
    // done once so the moving code never touches markers again.
    void cacheStages()
    {

        stageRoutines = new Coroutine[stages.Length];

        if (stageBase == null)
        {
            Debug.LogError("PlatformManager: No stage base marker given", this);
            return;
        }


        for (int i = 0; i < stages.Length; i++)
        {
            platformStage stage = stages[i];

            // stage starts unusable, only flipped on once setup finishes
            stage.ready = false;

            // catches a fat fingered array size in the inspector, checked before anything allocates
            if (stage.platforms == null || stage.platforms.Length > 256)
            {
                Debug.LogError("stage " + i + " has a bad platforms array, check the array size", this);
                continue;
            }

            if (stage.topMarker == null)
            {
                Debug.LogError("stage " + i + " has no top marker", this);
                continue;
            }

            stage.startPositions = new Vector3[stage.platforms.Length];
            stage.current = new float[stage.platforms.Length];
            stage.platformRoutines = new Coroutine[stage.platforms.Length];
            stage.order = new int[stage.platforms.Length];
            stage.riseHeight = stage.topMarker.position.y - stageBase.position.y;

            for (int j = 0; j < stage.platforms.Length; j++)
            {
                stage.order[j] = j;

                if (stage.platforms[j] == null)
                    continue;

                // snap down to the base height so everything starts even
                Vector3 pos = stage.platforms[j].position;
                pos.y = stageBase.position.y;
                stage.platforms[j].position = pos;
                stage.startPositions[j] = pos;
            }

            stage.ready = true;
        }
    }


    [ContextMenu("Rise Platforms")]
    // sends every stage up
    public void RisePlatforms()
    {
        startPass(true);
    }


    [ContextMenu("Fall Platforms")]
    // sends every stage back down
    public void FallPlatforms()
    {
        startPass(false);
    }


    // cancels whatever pass is running and starts a new one in the given direction
    void startPass(bool rising)
    {
        stopEveryPass();
        passRoutine = StartCoroutine(runPass(rising));
    }

    // stops the pass and every per stage and per platform routine underneath it,
    // so nothing survives a phase handoff
    void stopEveryPass()
    {
        if (passRoutine != null)
        {
            StopCoroutine(passRoutine);
            passRoutine = null;
        }

        if (stageRoutines == null)
        {
            return;
        }

        for (int i = 0; i < stageRoutines.Length; i++)
        {
            if (stageRoutines[i] != null)
            {
                StopCoroutine(stageRoutines[i]);
                stageRoutines[i] = null;
            }
        }
    }


    // walks the stages in order, waiting stageStagger between each
    IEnumerator runPass(bool rising)
    {
        for (int i = 0; i < stages.Length; i++)
        {
            stageRoutines[i] = StartCoroutine(runStage(stages[i], rising));

            if (stageStagger > 0f)
                yield return new WaitForSecondsRealtime(stageStagger);
        }

        passRoutine = null;
    }


    // moves one stage's platforms, waiting inStageStagger between each
    IEnumerator runStage(platformStage stage, bool rising)
    {
        // stage failed setup, skip it instead of throwing
        if (!stage.ready)
            yield break;

        // no stagger means the whole stage goes at once
        if (inStageStagger <= 0f)
        {
            for (int p = 0; p < stage.platforms.Length; p++)
                movePlatform(stage, p, rising);

            yield break;
        }

        // shuffle in place so the stage does not fire the same way every time
        for (int i = stage.order.Length - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            int temp = stage.order[i];
            stage.order[i] = stage.order[swap];
            stage.order[swap] = temp;
        }

        for (int i = 0; i < stage.order.Length; i++)
        {
            movePlatform(stage, stage.order[i], rising);

            yield return new WaitForSecondsRealtime(inStageStagger);
        }
    }


    // stops whatever this platform was doing before starting the new direction
    void movePlatform(platformStage stage, int index, bool rising)
    {
        if (stage.platforms[index] == null)
            return;

        if (stage.platformRoutines[index] != null)
            StopCoroutine(stage.platformRoutines[index]);

        stage.platformRoutines[index] = StartCoroutine(rising ? risePlatform(stage, index) : fallPlatform(stage, index));
    }


    // drives one platform up to the top
    IEnumerator risePlatform(platformStage stage, int index)
    {
        while (stage.current[index] < 1f)
        {

            float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            stage.current[index] = Mathf.MoveTowards(stage.current[index], 1f, stage.riseSpeed * step);
            applyHeight(stage, index);
            yield return null;
        }

        stage.platformRoutines[index] = null;
    }


    // drives one platform back down to the base
    IEnumerator fallPlatform(platformStage stage, int index)
    {
        while (stage.current[index] > 0f)
        {

            float step = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            stage.current[index] = Mathf.MoveTowards(stage.current[index], 0f, stage.fallSpeed * step);
            applyHeight(stage, index);
            yield return null;
        }

        stage.platformRoutines[index] = null;
    }


    // writes the current height onto the platform, called every frame while moving
    void applyHeight(platformStage stage, int index)
    {
        stage.platforms[index].position = stage.startPositions[index] + Vector3.up * (stage.current[index] * stage.riseHeight);
    }

}