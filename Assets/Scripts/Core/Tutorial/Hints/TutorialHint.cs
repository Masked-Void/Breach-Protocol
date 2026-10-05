using System;
using UnityEngine;

// one hint line and the rule for when it shows, it lives in TutorialHintSystem's list
[Serializable]
public class TutorialHint
{
    // what makes a hint show
    public enum TriggerType
    {
        // as soon as the space begins
        StageStart,

        // after the delay without moving
        Idle,

        // when a lesson raises the event key
        Event,

        // the delay after the space began, moving or not
        Timer
    }

    [Header("When")]
    [Tooltip("which space this hint belongs to")]
    [SerializeField] private TutorialStage stage;

    [Tooltip("stage start shows on entry, idle after the delay without moving, event when a lesson raises the key, timer the delay after entry")]
    [SerializeField] private TriggerType trigger;

    [Tooltip("real seconds for idle and timer hints")]
    [SerializeField] private float delaySeconds = 6f;

    [Tooltip("the key an event hint waits for, like dryFire or spikeReached")]
    [SerializeField] private string eventKey;

    [Tooltip("skips this hint if the key was already raised in this space, so a hint never explains something already done")]
    [SerializeField] private string skipIfEventRaised;

    [Header("What")]
    [Tooltip("the words, six or fewer, in the tutorial's voice")]
    [SerializeField] private string text;

    [Tooltip("speaker label for a handler framing, leave empty for none")]
    [SerializeField] private string speaker;

    [Tooltip("a higher priority replaces a lower hint that's already up, event hints also replace equal ones")]
    [SerializeField] private int priority;

    [Tooltip("real seconds it stays up")]
    [SerializeField] private float showSeconds = 3f;

    [Tooltip("only shows once per tutorial")]
    [SerializeField] private bool isOnceOnly = true;

    [Tooltip("hides it early when this key is raised, so doing the thing clears the prompt, moved and stopped are raised automatically")]
    [SerializeField] private string clearOnEventKey;

    public TutorialStage Stage => stage;

    public TriggerType Trigger => trigger;

    public float DelaySeconds => delaySeconds;

    public string EventKey => eventKey;

    public string SkipIfEventRaised => skipIfEventRaised;

    public string Text => text;

    public string Speaker => speaker;

    public int Priority => priority;

    public float ShowSeconds => showSeconds;

    public bool IsOnceOnly => isOnceOnly;

    public string ClearOnEventKey => clearOnEventKey;
}
