using UnityEngine;
using UnityEngine.Events;

/*
 * Script: TutorialManager
 *
 * Description:
 * Runs the concentric tutorial from the inside out. Each space's lesson has to
 * finish before its boundary starts dissolving, and the boundary still needs the
 * player moving to open, because walls dissolve on scaled time.
 *
 * Interacts With:
 * - TutorialLesson (every space's lesson registers here)
 * - RingBoundary (one per space, cell first)
 * - TutorialServices (tutorial mode and the adapters to the real game)
 * - TutorialExit (saving and loading the title), TutorialSkip (skip and debug keys)
 * - GameManager (PlayerReady and the player)
 *
 * Notes:
 * - Pressing play from this scene means the Bootstrap player doesn't exist during
 *   Start, so beginning also listens for GameManager.PlayerReady.
 * - A duplicate destroys only its component, since the services, exit and hint
 *   system share this object.
 */
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    // cell, bpm ring, weapon ring, shop ring
    private const int SpaceCount = 4;

    [Header("Spaces")]
    [Tooltip("one boundary per space from the inside out: cell, bpm ring, weapon ring, shop ring")]
    [SerializeField] private RingBoundary[] ringBoundaries = new RingBoundary[SpaceCount];

    [Tooltip("where the player goes back to in each space, same order, the cell's should be the Player Spawn Pos")]
    [SerializeField] private Transform[] respawnPoints = new Transform[SpaceCount];

    [Tooltip("falling below this height puts the player back at the current space's respawn point, meters")]
    [SerializeField] private float fallCatchHeightMeters = -10f;

    [Header("Links")]
    [Tooltip("tutorial mode and the adapters to the real game, on this same object")]
    [SerializeField] private TutorialServices services;

    [Tooltip("saves and loads the title when the tutorial ends, on this same object")]
    [SerializeField] private TutorialExit exit;

    [Header("Events")]
    [Tooltip("fires when the player moves out to a new space, passes the new stage")]
    public UnityEvent<TutorialStage> StageChanged = new UnityEvent<TutorialStage>();

    [Tooltip("fires when a lesson is accepted and its boundary starts dissolving")]
    public UnityEvent<TutorialStage> LessonCompleted = new UnityEvent<TutorialStage>();

    [Tooltip("fires when the player hits max bpm and the space restarts")]
    public UnityEvent<TutorialStage> RingRestarted = new UnityEvent<TutorialStage>();

    [Tooltip("fires once when the tutorial ends, finished or skipped")]
    public UnityEvent TutorialFinished = new UnityEvent();

    private readonly TutorialLesson[] lessons = new TutorialLesson[SpaceCount];
    private TutorialStage currentStage = TutorialStage.TimeCell;
    private UnityAction[] boundaryHandlers;
    private Transform player;
    private bool hasBegun;
    private bool isFinished;
    private bool isCurrentLessonComplete;

    public TutorialStage CurrentStage => currentStage;

    public TutorialServices Services => services;

    public bool IsFinished => isFinished;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;

        // built once so OnDisable removes the same handlers OnEnable added
        boundaryHandlers = new UnityAction[ringBoundaries.Length];
        for (int i = 0; i < ringBoundaries.Length; i++)
        {
            int index = i;
            boundaryHandlers[i] = () => handleBoundaryDissolved(index);
        }
    }

    private void OnEnable()
    {
        setListening(true);
    }

    private void OnDisable()
    {
        setListening(false);
    }

    private void Start()
    {
        // the usual flow loads this level after Bootstrap, so the player is already there
        if (instance == this && GameManager.instance != null && GameManager.instance.player != null)
        {
            beginTutorial();
        }
    }

    private void Update()
    {
        // one height check instead of a catch volume under every space
        if (hasBegun && !isFinished && player != null && player.position.y < fallCatchHeightMeters)
        {
            returnPlayerToSpace();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // lessons call this from their Start so the manager knows about them
    public void RegisterLesson(TutorialLesson lesson)
    {
        if (lesson == null || (int)lesson.Stage >= SpaceCount)
        {
            Debug.LogError("TutorialManager: a lesson registered for a space that doesn't exist", lesson);
            return;
        }

        lessons[(int)lesson.Stage] = lesson;

        // a lesson whose Start ran after the tutorial began still gets its turn
        if (hasBegun && !isFinished && lesson.Stage == currentStage && !lesson.IsRunning)
        {
            lesson.BeginLesson();
        }
    }

    // lessons call this once the player has done the thing, false if it wasn't that lesson's turn
    public bool CompleteLesson(TutorialStage stage)
    {
        // the cell's wall is its lesson and already opening, so there's nothing to accept
        if (!hasBegun || isFinished || stage != currentStage || stage == TutorialStage.TimeCell || isCurrentLessonComplete)
        {
            return false;
        }

        RingBoundary boundary = ringBoundaries[(int)stage];
        if (boundary == null)
        {
            Debug.LogError("TutorialManager: no boundary for " + stage + ", the player can't get out", this);
            return false;
        }

        isCurrentLessonComplete = true;
        boundary.StartDissolve();
        LessonCompleted.Invoke(stage);
        return true;
    }

    // the stress adapter's HeartFailed runs this
    public void RestartCurrentRing()
    {
        if (!hasBegun || isFinished)
        {
            return;
        }

        returnPlayerToSpace();
        if (services != null && services.Stress != null)
        {
            services.Stress.ResetToResting();
        }

        // a boundary already opening closes again, so the second try still means something
        if (currentStage != TutorialStage.TimeCell && ringBoundaries[(int)currentStage] != null)
        {
            ringBoundaries[(int)currentStage].ResetBoundary();
        }

        isCurrentLessonComplete = false;
        TutorialLesson lesson = lessons[(int)currentStage];
        if (lesson != null)
        {
            lesson.EndLesson();
            lesson.ResetLesson();
            lesson.BeginLesson();
        }

        RingRestarted.Invoke(currentStage);
    }

    // TutorialSkip's hold calls this
    public void SkipTutorial()
    {
        if (hasBegun)
        {
            finish(true);
        }
    }

    // TutorialSkip's debug key calls this, opens the current space's boundary at once
    public void OpenCurrentSpace()
    {
        RingBoundary boundary = hasBegun && !isFinished ? ringBoundaries[(int)currentStage] : null;
        if (boundary != null)
        {
            boundary.StartDissolve();
            boundary.FinishNow();
        }
    }

    // removes first every time, so a second call never doubles a listener
    private void setListening(bool isListening)
    {
        if (instance != this)
        {
            return;
        }

        for (int i = 0; i < ringBoundaries.Length; i++)
        {
            if (ringBoundaries[i] == null)
            {
                continue;
            }

            ringBoundaries[i].BoundaryDissolved.RemoveListener(boundaryHandlers[i]);
            if (isListening)
            {
                ringBoundaries[i].BoundaryDissolved.AddListener(boundaryHandlers[i]);
            }
        }

        GameManager.PlayerReady -= beginTutorial;
        ITutorialStress stress = services != null ? services.Stress : null;
        if (stress != null)
        {
            stress.HeartFailed -= RestartCurrentRing;
        }

        if (isListening)
        {
            GameManager.PlayerReady += beginTutorial;
            if (stress != null)
            {
                stress.HeartFailed += RestartCurrentRing;
            }
        }
    }

    private void beginTutorial()
    {
        if (hasBegun || isFinished)
        {
            return;
        }

        hasBegun = true;
        currentStage = TutorialStage.TimeCell;
        player = GameManager.instance != null && GameManager.instance.player != null ? GameManager.instance.player.transform : null;

        if (services != null)
        {
            services.EnterTutorialMode();
        }

        // LevelLoader places the player too, but not when play was pressed from this scene
        returnPlayerToSpace();
        StageChanged.Invoke(currentStage);
        beginCurrentLesson();

        // the cell's boundary is the time lesson, so it dissolves from the first frame
        if (ringBoundaries[0] != null)
        {
            ringBoundaries[0].StartDissolve();
        }
    }

    private void handleBoundaryDissolved(int index)
    {
        // only the current space's boundary moves the player on
        if (!hasBegun || isFinished || index != (int)currentStage)
        {
            return;
        }

        endCurrentLesson();
        currentStage = (TutorialStage)(index + 1);
        isCurrentLessonComplete = false;
        StageChanged.Invoke(currentStage);

        if (currentStage == TutorialStage.Finished)
        {
            finish(false);
        }
        else
        {
            beginCurrentLesson();
        }
    }

    private void finish(bool isSkip)
    {
        if (isFinished)
        {
            return;
        }

        isFinished = true;
        endCurrentLesson();
        TutorialFinished.Invoke();

        if (exit != null)
        {
            exit.Leave(isSkip);
        }
    }

    private void beginCurrentLesson()
    {
        TutorialLesson lesson = lessons[(int)currentStage];
        if (lesson != null)
        {
            lesson.BeginLesson();
        }
    }

    private void endCurrentLesson()
    {
        TutorialLesson lesson = currentStage == TutorialStage.Finished ? null : lessons[(int)currentStage];
        if (lesson != null && lesson.IsRunning)
        {
            lesson.EndLesson();
        }
    }

    private void returnPlayerToSpace()
    {
        if (respawnPoints.Length == 0 || services == null || services.Movement == null)
        {
            return;
        }

        Transform point = respawnPoints[Mathf.Min((int)currentStage, respawnPoints.Length - 1)];
        if (point != null)
        {
            services.Movement.TeleportTo(point.position, point.rotation);
        }
    }
}
