using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenManager : MonoBehaviour
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
    [SerializeField] private GameObject SoundMenu;
    [SerializeField] private GameObject controlsMenu;
    [SerializeField] private Slider progressBar;

    void Start()
    {
        Time.timeScale = 1f;
        switchToHome();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        AudioManager.instance.playTitleScreenSound();
    }

    public void openLevelSamuel()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("MK2"));
    }
    public void openLevelDevinS()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("Devin"));
    }
    public void openLevelDevinC()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("dclevel"));
    }
    public void openLevelMark()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("Mark"));
    }
    public void openLevelKhurshed()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("ColdStorage"));
    }
    public void openLevelVirel()
    {
        AudioManager.instance.playButtonClick();
        Nav.SetActive(false);
        deactivateAllPanels();
        StartCoroutine(LoadSceneAsync("LevelCreation-Virel"));
    }

    public void openSettings()
    {
        AudioManager.instance.playButtonClick();
        deactivateAllSettings();
        SoundMenu.SetActive(true);
    }

    public void controls()
    {
        AudioManager.instance.playButtonClick();
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

        if (AudioManager.instance != null) AudioManager.instance.stopMusic();

        scene.allowSceneActivation = true;
    }

    public void quitGame()
    {
        AudioManager.instance.playButtonClick();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            Application.OpenURL("about:blank");
        #else
            Application.Quit();
        #endif
    }

    public void switchToHome()
    {
        AudioManager.instance.playButtonClick();
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
        AudioManager.instance.playButtonClick();
        deactivateAllPanels();
        settingsPanel.SetActive(true);
    }

    public void switchToAbout()
    {
        AudioManager.instance.playButtonClick();
        deactivateAllPanels();
        aboutPanel.SetActive(true);
    }

    public void switchToCredits()
    {
        AudioManager.instance.playButtonClick();
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
        SoundMenu.SetActive(false);
        controlsMenu.SetActive(false);
    }
}