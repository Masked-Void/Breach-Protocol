using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeButtonData : MonoBehaviour
{
    public UpgradeData upgrade;
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
        if (AudioManager.instance != null)
            AudioManager.instance.PlayButtonClick();
                     
        if (UpgradeManager.instance != null && upgrade != null)
            UpgradeManager.instance.DisplayUpgrades(upgrade);
    }
}
