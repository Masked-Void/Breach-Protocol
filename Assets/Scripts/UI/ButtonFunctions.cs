using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonFunctions : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingBackground;
    [SerializeField] private Slider progressBar;

    public void resume()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        if (GameManager.instance != null) GameManager.instance.stateUnpause();
    }

    public void restart()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        StartCoroutine(LoadSceneAsync(SceneManager.GetActiveScene().name));
    }

    public void home()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        StartCoroutine(LoadSceneAsync("Title"));
    }

    public IEnumerator LoadSceneAsync(String levelName)
    {
        if (loadingBackground != null) loadingBackground.SetActive(true);

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

        if (progressBar != null) progressBar.value = 1f;

        yield return new WaitForSecondsRealtime(0.2f);

        if (AudioManager.instance != null) AudioManager.instance.stopMusic();
        Time.timeScale = 1f;
        if (GameManager.instance != null) GameManager.instance.stateUnpause();
        scene.allowSceneActivation = true;
    }

    public void challenge()
    {
        deactivateAllPanels();
        if (GameManager.instance.challengesCanvas != null) GameManager.instance.challengesCanvas.SetActive(true);
    }

    public void upgrade()
    {
        deactivateAllPanels();
        if (GameManager.instance.upgradesCanvas != null) GameManager.instance.upgradesCanvas.SetActive(true);
    }

    public void options()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (GameManager.instance.backButton != null) GameManager.instance.backButton.SetActive(true);
        if (GameManager.instance.navTab != null) GameManager.instance.navTab.SetActive(true);
        if (GameManager.instance.settingsCanvas != null) GameManager.instance.settingsCanvas.SetActive(true);
    }

    public void settings()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (GameManager.instance.settingsCanvas != null) GameManager.instance.settingsCanvas.SetActive(true);
    }

    public void sound()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        if (GameManager.instance.controlsMenu != null) GameManager.instance.controlsMenu.SetActive(false);
        if (GameManager.instance.SoundMenu != null) GameManager.instance.SoundMenu.SetActive(true);
    }

    public void controls()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        if (GameManager.instance.SoundMenu != null) GameManager.instance.SoundMenu.SetActive(false);
        if (GameManager.instance.controlsMenu != null) GameManager.instance.controlsMenu.SetActive(true);
    }

    public void pauseBack()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (GameManager.instance.backButton != null) GameManager.instance.backButton.SetActive(false);
        if (GameManager.instance.navTab != null) GameManager.instance.navTab.SetActive(false);
        if (GameManager.instance.buttons != null) GameManager.instance.buttons.SetActive(true);
        if (GameManager.instance.pauseScorePanel != null) GameManager.instance.pauseScorePanel.SetActive(true);
    }

    private void deactivateAllPanels()
    {
        if (GameManager.instance == null) return;
        if (GameManager.instance.challengesCanvas != null) GameManager.instance.challengesCanvas.SetActive(false);
        if (GameManager.instance.settingsCanvas != null) GameManager.instance.settingsCanvas.SetActive(false);
        if (GameManager.instance.upgradesCanvas != null) GameManager.instance.upgradesCanvas.SetActive(false);
        if (GameManager.instance.SoundMenu != null) GameManager.instance.SoundMenu.SetActive(false);
        if (GameManager.instance.controlsMenu != null) GameManager.instance.controlsMenu.SetActive(false);
        if (GameManager.instance.buttons != null) GameManager.instance.buttons.SetActive(false);
        if (GameManager.instance.pauseScorePanel != null) GameManager.instance.pauseScorePanel.SetActive(false);
    }


    public void quit()
    {
        if (AudioManager.instance != null) AudioManager.instance.playButtonClick();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            Application.OpenURL("about:blank");
        #else
            Application.Quit();
        #endif
    }
}