using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// leaves the tutorial. saves the flags, takes the game out of tutorial mode, lets the player look at the arena, then loads the title
public class TutorialExit : MonoBehaviour
{
    [Header("Links")]
    [Tooltip("tutorial mode and the adapters, on this same object")]
    [SerializeField] private TutorialServices services;

    [Header("Leaving")]
    [Tooltip("real seconds to look at the full arena after finishing, real time so standing still can't stall it, skipping leaves at once")]
    [SerializeField] private float revealSeconds = 4f;

    [Tooltip("scene loaded when the tutorial ends, the same one the pause menu's home button loads")]
    [SerializeField] private string titleSceneName = "Title";

    private Coroutine leaveRoutine;

    public bool IsLeaving => leaveRoutine != null;

    public void Leave(bool wasSkipped)
    {
        if (leaveRoutine == null)
        {
            leaveRoutine = StartCoroutine(LeaveAfterReveal(wasSkipped));
        }
    }

    private IEnumerator LeaveAfterReveal(bool wasSkipped)
    {
        TutorialSaveBridge.MarkPlayed();
        if (!wasSkipped)
        {
            TutorialSaveBridge.MarkTutorialComplete();
        }

        if (services != null)
        {
            services.ExitTutorialMode();
        }

        if (!wasSkipped && revealSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(revealSeconds);
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.StopMusic();
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.StateUnpause();
        }

        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(titleSceneName);
    }
}
