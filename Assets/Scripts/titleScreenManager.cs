using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class titleScreenManager : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject homePanel;
    public GameObject weaponPanel;
    public GameObject settingsPanel;
    public GameObject aboutPanel;

    [Header("Top Navigation Buttons")]
    public GameObject Nav;
    public Button navHomeButton;
    public Button navWeaponButton;
    public Button navSettingsButton;
    public Button navAboutButton;
    public Button navCreditsButton;

    [Header("Levels")]
    public Button LevelSamuel;
    public Button levelDevinS;
    public Button levelDevinC;
    public Button levelMark;
    public Button levelKhurshed;
    public Button levelVirel;

    [SerializeField] private GameObject titleMenuPanel;
    [SerializeField] private GameObject soundMenu;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject unlockShop;
    CanvasGroup canvasGroup;

    void Start()
    {
        switchToHome();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        canvasGroup = titleMenuPanel.GetComponent<CanvasGroup>();

        audioManager.instance.playTitleScreenSound();
    }

     public void openLevelSamuel()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("MK2"));
    }
    public void openLevelDevinS()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("MK2"));
    }
    public void openLevelDevinC()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("MK2"));
    }
    public void openLevelMark()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("Mark"));
    }
    public void openLevelKhurshed()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("MK2"));
    }
    public void openLevelVirel()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("Virel"));
    }

    public void openSettings()
    {
        audioManager.instance.playButtonClick();
        deactivateAllSettings();
        soundMenu.SetActive(true);
    }

    private IEnumerator LoadSceneAsync(String levelName)
    {
        /*if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * 2f;
                yield return null;
            }
        }*/

        deactivateAllPanels();

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        AsyncOperation scene = SceneManager.LoadSceneAsync(levelName);
        scene.allowSceneActivation = false;

        while (scene.progress < 0.9f)
        {
            float progressValue = Mathf.Clamp01(scene.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progressValue;
            }

            yield return null;
        }

        if (progressBar != null)
        {
            progressBar.value = 1f;
        }

        yield return new WaitForSeconds(0.2f);

        if (audioManager.instance != null) audioManager.instance.stopMusic();

        scene.allowSceneActivation = true;
    }

    public void quitGame()
    {
        audioManager.instance.playButtonClick();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void openUnlocks()
    {
        audioManager.instance.playButtonClick();
        deactivateAllSettings();
        unlockShop.SetActive(true);
    }

    public void closeUnlocks()
    {
        unlockShop.SetActive(false);
    }

    public void switchToHome()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();
        homePanel.SetActive(true);
    }

    public void switchToWeapon()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();
        weaponPanel.SetActive(true);
    }

    public void switchToSettings()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();
        settingsPanel.SetActive(true);
    }

    public void switchToAbout()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();
        aboutPanel.SetActive(true);
    }

    private void deactivateAllPanels()
    {
        homePanel.SetActive(false);
        weaponPanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    private void deactivateAllSettings()
    {
        soundMenu.SetActive(false);
        unlockShop.SetActive(false);
    }
}