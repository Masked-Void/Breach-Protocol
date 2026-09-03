using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeButtonData : MonoBehaviour
{
    [Tooltip("which weapon's challenge set this button opens")]
    public ChallengeData challenge;

    [Tooltip("shown over the button until every challenge in the set is done")]
    public GameObject lockIcon;

    [Tooltip("tick on one button so the panel opens with something already selected")]
    [SerializeField] private bool selectOnEnable = false;

    Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Selected);
    }

    // waits a frame before reading state, so ChallengeManager has finished
    // loading saved progress before the lock icon is set
    IEnumerator initButton()
    {
        yield return null;
        updateLockstate();
        if (selectOnEnable)
        {
            Selected();
        }
    }

    // shows the lock unless every challenge in the set is complete
    void updateLockstate()
    {
        if (ChallengeManager.instance == null || challenge == null) return;
        bool allcomplete = ChallengeManager.instance.AreAllChallengesComplete(challenge);
        if (lockIcon != null) lockIcon.SetActive(!allcomplete);
    }

    void OnEnable()
    {
        // Whenever the challenge panel opens, auto-click this button if marked as default
        StartCoroutine(initButton());
    }

    // opens this weapon's challenges in the panel
    void Selected()
    {   
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();
                     
        if (ChallengeManager.instance != null && challenge != null)
            ChallengeManager.instance.DisplayWeaponChallenges(challenge);
    }
}
