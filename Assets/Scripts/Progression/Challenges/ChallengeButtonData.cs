using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeButtonData : MonoBehaviour
{
    public ChallengeData challenge;
    Button button;
    public GameObject lockIcon;
    [SerializeField] private bool selectOnEnable = false;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Selected);
    }

    IEnumerator initButton()
    {
        yield return null;
        updateLockstate();
        if (selectOnEnable)
        {
            Selected();
        }
    }

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

    void Selected()
    {   
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();
                     
        if (ChallengeManager.instance != null && challenge != null)
            ChallengeManager.instance.DisplayWeaponChallenges(challenge);
    }
}
