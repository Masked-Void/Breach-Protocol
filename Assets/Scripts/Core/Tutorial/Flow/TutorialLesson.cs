using UnityEngine;

public abstract class TutorialLesson : MonoBehaviour
{
    private bool isRunning;
    private bool isComplete;

    public abstract TutorialStage Stage { get; }

    public bool IsRunning => isRunning;
    public bool IsComplete => isComplete;

    protected TutorialServices Services => TutorialManager.instance != null ? TutorialManager.instance.Services : null;

    protected virtual void Start()
    {
        if (TutorialManager.instance == null)
        {
            Debug.LogError("TutorialLesson: no TutorialManager in the scene", this);
            return;
        }

        TutorialManager.instance.RegisterLesson(this);
    }

    public virtual void BeginLesson()
    {
        isRunning = true;
        isComplete = false;
    }

    public virtual void EndLesson()
    {
        isRunning = false;
    }

    public virtual void ResetLesson()
    {
        isComplete = false;
    }

    protected void Complete()
    {
        if (!isRunning || isComplete)
        {
            return;
        }

        isComplete = TutorialManager.instance != null && TutorialManager.instance.CompleteLesson(Stage);
    }

    protected void RaiseHint(string eventKey)
    {
        //if (TutorialHintSystem)
    }
}
