using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class upgradeButtonData : MonoBehaviour
{
    public upgradeData upgrade;
    Button button;
    [SerializeField] private bool selectOnEnable = false;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Selected);
    }

    void OnEnable()
    {
        // Whenever the upgrade panel opens, auto-click this button if marked as default
        StartCoroutine(initButton());
    }

    IEnumerator initButton()
    {
        yield return null;
        if (selectOnEnable)
        {
            Selected();
        }
    }

    // Handle weapon selection
    void Selected()
    {   
        if (audioManager.instance != null)
            audioManager.instance.playButtonClick();
                     
        if (upgradeManager.instance != null && upgrade != null)
            upgradeManager.instance.displayUpgrades(upgrade);
    }
}
