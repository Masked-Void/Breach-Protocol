using UnityEngine;

// placeholder lesson for greyboxing, standing in the volume counts as doing the lesson
[RequireComponent(typeof(Collider))]
public class TutorialZone : TutorialLesson
{
    [Header("Zone")]
    [SerializeField] private TutorialStage stage = TutorialStage.BpmRing;

    [SerializeField] private string playerTag = "Player";

    public override TutorialStage Stage => stage;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsRunning || !other.CompareTag(playerTag))
        {
            return;
        }

        Complete();
    }
}
