using UnityEngine;
using UnityEngine.SceneManagement;

public class buttonFunctions : MonoBehaviour
{
    public void resume()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
    }

    public void restart()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void home()
    {
        if (audioManager.instance != null) audioManager.instance.playButtonClick();
        if (gameManager.instance != null) gameManager.instance.stateUnpause();
        SceneManager.LoadScene(0);
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
        #else
            Application.Quit();
        #endif
    }
}