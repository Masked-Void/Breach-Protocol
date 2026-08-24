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
    public GameObject challengePanel;
    public GameObject settingsPanel;
    public GameObject aboutPanel;
    public GameObject creditsPanel;

    [Header("Top Navigation Buttons")]
    public GameObject Nav;
    public Button navHomeButton;
    public Button navWeaponButton;
    public Button navSettingsButton;
    public Button navAboutButton;
    public Button navCreditsButton;

    [SerializeField] private GameObject titleMenuPanel;
    [SerializeField] private GameObject soundMenu;
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private Slider progressBar;

    void Start()
    {
        Time.timeScale = 1f;
        switchToHome();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

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
        StartCoroutine(LoadSceneAsync("Devin"));
    }
    public void openLevelDevinC()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("Mark"));
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
        StartCoroutine(LoadSceneAsync("ColdStorage"));
    }
    public void openLevelVirel()
    {
        audioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("LevelCreation-Virel"));
    }

    public void openSettings()
    {
        audioManager.instance.playButtonClick();
        deactivateAllSettings();
        soundMenu.SetActive(true);
    }

    public void controls()
    {
        audioManager.instance.playButtonClick();
        deactivateAllSettings();
        controlsMenu.SetActive(true);
    }

    private IEnumerator LoadSceneAsync(String levelName)
    {
        deactivateAllSettings();
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

        yield return new WaitForSecondsRealtime(0.2f);

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

    public void switchToHome()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();
        homePanel.SetActive(true);
    }
    public void switchToChallenge()
    {
        deactivateAllPanels();
        challengePanel.SetActive(true);
    }
    public void switchToWeapon()
    {
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

    public void switchToCredits()
    {
        audioManager.instance.playButtonClick();
        deactivateAllPanels();

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
        }
    }

    private void deactivateAllPanels()
    {
        homePanel.SetActive(false);
        weaponPanel.SetActive(false);
        settingsPanel.SetActive(false);
        challengePanel.SetActive(false);

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(false);
        }

        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    private void deactivateAllSettings()
    {
        soundMenu.SetActive(false);
        controlsMenu.SetActive(false);
    }
}