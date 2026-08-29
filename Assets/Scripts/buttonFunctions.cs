using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class buttonFunctions : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingBackground;
    [SerializeField] private Slider progressBar;

    public void resume()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
    }

    public void restart()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
        StartCoroutine(LoadSceneAsync(SceneManager.GetActiveScene().name));
    }

    public void home()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
        StartCoroutine(LoadSceneAsync("Title"));
    }

    public IEnumerator LoadSceneAsync(String levelName)
    {
        Time.timeScale = 1f;

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

        if (audioManager.instance != null) audioManager.instance.stopMusic();
        scene.allowSceneActivation = true;
        //if (loadingBackground != null) loadingBackground.SetActive(false);
    }

    public void challenge()
    {
        deactivateAllPanels();
        if (gameManager.instance.challengesCanvas != null) gameManager.instance.challengesCanvas.SetActive(true);
    }

    public void upgrade()
    {
        deactivateAllPanels();
        if (gameManager.instance.upgradesCanvas != null) gameManager.instance.upgradesCanvas.SetActive(true);
    }

    public void options()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (gameManager.instance.backButton != null) gameManager.instance.backButton.SetActive(true);
        if (gameManager.instance.navTab != null) gameManager.instance.navTab.SetActive(true);
        if (gameManager.instance.settingsCanvas != null) gameManager.instance.settingsCanvas.SetActive(true);
    }

    public void settings()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (gameManager.instance.settingsCanvas != null) gameManager.instance.settingsCanvas.SetActive(true);
    }

    public void sound()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance.controlsMenu != null) gameManager.instance.controlsMenu.SetActive(false);
        if (gameManager.instance.soundMenu != null) gameManager.instance.soundMenu.SetActive(true);
    }

    public void controls()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance.soundMenu != null) gameManager.instance.soundMenu.SetActive(false);
        if (gameManager.instance.controlsMenu != null) gameManager.instance.controlsMenu.SetActive(true);
    }

    public void pauseBack()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        deactivateAllPanels();
        if (gameManager.instance.backButton != null) gameManager.instance.backButton.SetActive(false);
        if (gameManager.instance.navTab != null) gameManager.instance.navTab.SetActive(false);
        if (gameManager.instance.buttons != null) gameManager.instance.buttons.SetActive(true);
        if (gameManager.instance.pauseScorePanel != null) gameManager.instance.pauseScorePanel.SetActive(true);
    }

    private void deactivateAllPanels()
    {
        if (gameManager.instance == null) return;
        if (gameManager.instance.challengesCanvas != null) gameManager.instance.challengesCanvas.SetActive(false);
        if (gameManager.instance.settingsCanvas != null) gameManager.instance.settingsCanvas.SetActive(false);
        if (gameManager.instance.upgradesCanvas != null) gameManager.instance.upgradesCanvas.SetActive(false);
        if (gameManager.instance.soundMenu != null) gameManager.instance.soundMenu.SetActive(false);
        if (gameManager.instance.controlsMenu != null) gameManager.instance.controlsMenu.SetActive(false);
        if (gameManager.instance.buttons != null) gameManager.instance.buttons.SetActive(false);
        if (gameManager.instance.pauseScorePanel != null) gameManager.instance.pauseScorePanel.SetActive(false);
    }


    public void quit()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBGL
            Application.OpenURL("about:blank");
        #else
            Application.Quit();
        #endif
    }
}