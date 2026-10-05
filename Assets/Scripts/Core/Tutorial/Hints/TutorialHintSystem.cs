using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/*
 * Script: TutorialHintSystem
 *
 * Description:
 * Picks which hint shows and when, one at a time. Every timer here runs on real
 * time, because a player standing still is the one who needs a hint, and standing
 * still slows the world to its floor.
 *
 * Responsibilities:
 * - Show stage start hints on entry, and idle and timer hints once their delay passes
 * - Show event hints when a lesson raises their key, and clear hints on their clear key
 * - Raise moved and stopped itself, from the activity tracker
 *
 * Interacts With:
 * - TutorialManager (StageChanged, RingRestarted)
 * - TutorialLesson (RaiseEvent)
 * - TutorialPromptUI (shows the words), PlayerActivityTracker (idle time)
 *
 * Notes:
 * - The cooldown only paces idle and timer hints. Event hints answer something the
 *   player just did, so they show straight away.
 */

public class TutorialHintSystem : MonoBehaviour
{
    public static TutorialHintSystem instance;

    public const string MovedKey = "moved";
    public const string StoppedKey = "stopped";
    public const string RingRestartedKey = "ringRestarted";

    [Header("Hints")]
    [Tooltip("every hint line in the tutorial")]
    [SerializeField] private List<TutorialHint> hints = new List<TutorialHint>();

    [Tooltip("Real seconds between one idle or timer hint ending and the next starting")]
    [SerializeField] private float cooldownSeconds = 2f;

    [Header("Links")]
    [Tooltip("The manager on this same object")]
    [SerializeField] TutorialManager manager;

    [Tooltip("Shows the words")]
    [SerializeField] private TutorialPromptUI promptUI;

    [Tooltip("Where idle time comes from, on this same object")]
    [SerializeField] private PlayerActivityTracker activity;

    [Header("Events")]
    [Tooltip("fires each time a hint shows")]
    public UnityEvent<TutorialHint> HintShown = new UnityEvent<TutorialHint>();

    private readonly HashSet<TutorialHint> shownHints = new HashSet<TutorialHint>();
    private readonly HashSet<string> raisedThisStage = new HashSet<string>();
    private TutorialStage currentStage;
    private TutorialHint activeHint;
    private float stageStartTime;
    private float activeHintEndTime;
    private float lastHintEndTime = -999f;
    private bool hasStage;
    private bool wasMoving;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void OnEnable()
    {
        if (manager != null)
        {
            manager.StageChanged.AddListener(handleStageChanged);
            manager.RingRestarted.AddListener(handleRingRestarted);
        }
    }

    private void OnDisable()
    {
        if (manager != null)
        {
            manager.StageChanged.RemoveListener(handleStageChanged);
            manager.RingRestarted.RemoveListener(handleRingRestarted);
        }
    }

    private void Update()
    {
        bool isPaused = GameManager.instance != null && GameManager.instance.isPaused;

        if (!hasStage || isPaused || (manager!= null && manager.IsFinished))
        {
            return;
        }

        float now = Time.unscaledTime;

        if (activeHint != null && now >= activeHintEndTime)
        {
            activeHint = null;
            lastHintEndTime = now;
        }

        raiseMovementKeys();

        foreach (TutorialHint hint in hints)
        {
            if (hint == null || hint.Stage != currentStage)
            {
                continue;
            }

            bool isIdleDue = hint.Trigger == TutorialHint.TriggerType.Idle && activity != null && activity.IdleSeconds >= hint.DelaySeconds;
            bool isTimerDue = hint.Trigger == TutorialHint.TriggerType.Timer && now - stageStartTime >= hint.DelaySeconds;

            if (isIdleDue || isTimerDue)
            {
                tryShow(hint);
            }
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // lessons call tyhis with keys like spikeReached,dryFire,panelBroken
    public void RaiseEvent(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey))
        {
            return;
        }

        raisedThisStage.Add(eventKey);

        if (activeHint!=null && activeHint.ClearOnEventKey == eventKey)
        {
            HideActive();
        }

        foreach (TutorialHint hint in hints)
        {
            if (hint != null && hint.Stage == currentStage && hint.Trigger == TutorialHint.TriggerType.Event  && hint.EventKey == eventKey)
            {
                tryShow(hint);
            } 
        }
    }

    public void HideActive()
    {
        if (promptUI != null)
        {
            promptUI.Hide();
        }

        if (activeHint != null)
        {
            activeHint = null;
            lastHintEndTime = Time.unscaledTime;
        }
    }

    private void handleStageChanged(TutorialStage stage)
    {
        currentStage = stage;
        hasStage = true;
        startStageClock();

        foreach (TutorialHint hint in hints)
        {
            if (hint!=null && hint.Stage == stage && hint.Trigger == TutorialHint.TriggerType.StageStart)
            {
                tryShow(hint);
            }
        }
    }

    private void handleRingRestarted(TutorialStage stage)
    {
        startStageClock();
        RaiseEvent(RingRestartedKey);
    }

    // every hint in the new space counts from the entrance
    private void startStageClock()
    {
        stageStartTime = Time.unscaledTime;
        raisedThisStage.Clear();
        HideActive();

        if (activity != null)
        {
            activity.ResetIdle();
        }
    }

    private void raiseMovementKeys()
    {
        if (activity == null || activity.IsMoving == wasMoving)
        {
            return;
        }

        wasMoving = activity.IsMoving;
        RaiseEvent(wasMoving ? MovedKey : StoppedKey);
    }

    private void tryShow(TutorialHint hint)
    {
        if (hint == activeHint || (hint.IsOnceOnly && shownHints.Contains(hint)))
        {
            return;
        }

        if (!string.IsNullOrEmpty(hint.SkipIfEventRaised) && raisedThisStage.Contains(hint.SkipIfEventRaised))
        {
            return;
        }

        float now = Time.unscaledTime;
        bool isEvent = hint.Trigger == TutorialHint.TriggerType.Event;

        if (activeHint != null)
        {
            // only something more important replaces what's up, an event hint also replaces its equal
            bool isMoreImportant = isEvent ? hint.Priority >= activeHint.Priority : hint.Priority > activeHint.Priority;
            if (!isMoreImportant)
            {
                return;
            }
        }
        else if (!isEvent && now - lastHintEndTime < cooldownSeconds)
        {
            return;
        }

        activeHint = hint;
        activeHintEndTime = now + hint.ShowSeconds;
        shownHints.Add(hint);

        if(promptUI != null)
        {
            promptUI.Show(hint.Text, hint.ShowSeconds, hint.Speaker);
        }

        HintShown.Invoke(hint);
    }
}
