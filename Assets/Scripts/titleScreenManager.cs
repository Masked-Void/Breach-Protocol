using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class titleMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject titleMenuPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject Buttons;
    [SerializeField] private GameObject SoundMenu;
    CanvasGroup canvasGroup;

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        canvasGroup = titleMenuPanel.GetComponent<CanvasGroup>();

        audioManager.instance.playTitleScreenSound();
    }

    public void continueGame()
    {
        audioManager.instance.playButtonClick();
        StartCoroutine(LoadSceneAsync());
    }

    public void openSettings()
    {
        audioManager.instance.playButtonClick();
        Buttons.SetActive(false);
        SoundMenu.SetActive(true);
    }

    public void closeSettings()
    {
        audioManager.instance.playButtonClick();
        SoundMenu.SetActive(false);
        Buttons.SetActive(true);
    }

    private IEnumerator LoadSceneAsync()
    {
        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * 2f;
                yield return null;
            }
        }

        Buttons.SetActive(false);

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        AsyncOperation scene = SceneManager.LoadSceneAsync("MK2");
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
}