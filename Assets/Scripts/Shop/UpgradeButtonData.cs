using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// one upgrade button in the shop panel. clicking it shows that upgrade's details.
public class UpgradeButtonData : MonoBehaviour
{
    [Tooltip("which upgrade this button opens")]
    public UpgradeData upgrade;

    [Tooltip("tick on one button so the panel opens with something already selected")]
    [SerializeField] private bool selectOnEnable = false;

    Button button;

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

    // waits a frame before auto-selecting, so UpgradeManager has finished
    // loading before anything asks it for details
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
