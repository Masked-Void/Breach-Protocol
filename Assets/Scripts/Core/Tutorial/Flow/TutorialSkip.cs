using UnityEngine;
using UnityEngine.UI;

// the ways out of a space early. holding a key skips the whole tutorial, the hold stops a stray tap
// from skipping it, and a debug key opens the current space for testing
public class TutorialSkip : MonoBehaviour
{
    [Header("Skip")]
    [Tooltip("hold this to skip the tutorial, escape already opens the pause menu")]
    [SerializeField] private KeyCode skipKey = KeyCode.Backspace;

    [Tooltip("real seconds the key has to be held")]
    [SerializeField] private float holdSeconds = 1.5f;

    [Tooltip("shown while the key is held, like a hold to skip label, optional")]
    [SerializeField] private GameObject holdRoot;

    [Tooltip("filled image that shows how long the key has been held, optional")]
    [SerializeField] private Image holdFill;

    [Header("Debug")]
    [Tooltip("lets the debug key open the current space instantly, turn off for builds")]
    [SerializeField] private bool isDebugKeyAllowed = false;

    [Tooltip("opens the current space's boundary at once when allowed")]
    [SerializeField] private KeyCode debugKey = KeyCode.F9;

    [Header("Links")]
    [Tooltip("the manager on this same object")]
    [SerializeField] private TutorialManager manager;

    // real seconds held so far
    private float heldSeconds;

    private void Start()
    {
        setHoldVisible(false);
    }

    private void Update()
    {
        if (manager == null || manager.IsFinished)
        {
            return;
        }

        if (isDebugKeyAllowed && Input.GetKeyDown(debugKey))
        {
            manager.OpenCurrentSpace();
        }

        bool isPaused = GameManager.instance != null && GameManager.instance.isPaused;

        if(isPaused || !Input.GetKey(skipKey))
        {
            if (heldSeconds > 0f)
            {
                heldSeconds = 0f;
                setHoldVisible(false);
            }
            return;
        }

        heldSeconds += Time.unscaledDeltaTime;
        setHoldVisible(true);

        if (holdFill != null)
        {
            holdFill.fillAmount = Mathf.Clamp01(heldSeconds / Mathf.Max(0.01f, holdSeconds));
        }

        if (heldSeconds >= holdSeconds)
        {
            heldSeconds = 0f;
            setHoldVisible(false);
            manager.SkipTutorial();
        }
    }

    private void setHoldVisible(bool isVisible)
    {
        if (holdRoot != null  && holdRoot.activeSelf != isVisible)
        {
            holdRoot.SetActive(isVisible);
        }
    }

}
