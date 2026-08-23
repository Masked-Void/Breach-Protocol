using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class challengeButtonData : MonoBehaviour
{
    public challengeData challenge;
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
        if (challengeManager.instance == null || challenge == null) return;
        bool allcomplete = challengeManager.instance.areAllChallengesComplete(challenge);
        if (lockIcon != null)
        {
            lockIcon.SetActive(!allcomplete);
        }
    }

    void OnEnable()
    {
        // Whenever the challenge panel opens, auto-click this button if marked as default
        StartCoroutine(initButton());
    }

    void Selected()
    {            
        if (challengeManager.instance != null && challenge != null)
            challengeManager.instance.displayWeaponChallenges(challenge);
    }
}
