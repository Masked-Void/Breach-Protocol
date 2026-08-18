using UnityEngine;
using UnityEngine.UI;

public class challengeButtonData : MonoBehaviour
{
    public challengeData[] challenges;
    Button button;
    [SerializeField] private bool selectOnEnable = false;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Selected);
    }

    void OnEnable()
    {
        // Whenever the challenge panel opens, auto-click this button if marked as default
        if (selectOnEnable)
        {
            Selected();
        }
    }

    void Selected()
    {
        if (challengeManager.instance != null && challenges.Length != 0)
            challengeManager.instance.displayWeaponChallenges(challenges);
    }
}
