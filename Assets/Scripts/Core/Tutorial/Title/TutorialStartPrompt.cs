using UnityEngine;
using UnityEngine.Events;

// asks a fresh save whether to play the tutorial, on the title screen. both answers set the flag, so nobody is asked twice
public class TutorialStartPrompt : MonoBehaviour
{
    [Header("Links")]
    [Tooltip("the title screen, it loads the tutorial the same way it loads every level")]
    [SerializeField] private TitleScreenManager titleScreen;

    [Tooltip("The yes or no panel")]
    [SerializeField] private GameObject promptPanel;

    [Header("Timing")]
    [Tooltip("asks as soon as the title opens, turn off once a start button calls OnStartPressed")]
    [SerializeField] private bool shouldAskOnOpen = true;

    [Header("Events")]
    [Tooltip("Fires when the player picks yes")]
    public UnityEvent Accepted = new UnityEvent();
    [Tooltip("fires when the player picks no")]
    public UnityEvent Declined = new UnityEvent();

    private void Start()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (shouldAskOnOpen && !TutorialSaveBridge.HasPlayedBefore)
        {
            showPrompt(); 
        }
    }

    // a Start button calls this, a fresh save gets the question and everyone else goes to the home panel
    public void OnStartPress()
    {
        if (!TutorialSaveBridge.HasPlayedBefore)
        {
            showPrompt();
        }
        else if(titleScreen != null)
        {
            titleScreen.switchToHome();
        }
    }

    // yes button
    public void Accept()
    {
        TutorialSaveBridge.MarkPlayed();
        hidePrompt();
        Accepted.Invoke();

        if (titleScreen != null)
        {
            titleScreen.OpenTutorial();
        }
    }

    public void Decline()
    {
        TutorialSaveBridge.MarkPlayed();
        hidePrompt() ;
        Declined.Invoke();

        if (titleScreen != null)
        {
            titleScreen.switchToHome();
        }
    }

    private void showPrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }
    }

    private void hidePrompt()
    {
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }
    }
}
