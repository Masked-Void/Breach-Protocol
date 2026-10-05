using System.Collections;
using TMPro;
using UnityEngine;

// shows one short prompt at a time and fades it, all on real time. keep its object active, a hidden prompt is just faded out
public class TutorialPromptUI : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("The prompt words")]
    [SerializeField] private TMP_Text promptText;

    [Tooltip("speaker label for voiced tutorial")]
    [SerializeField] private TMP_Text speakerText;

    [Tooltip("fades the whole prompt")]
    [SerializeField] private CanvasGroup group;

    [Header("Timing")]
    [Tooltip("Real secoinds to faade in or out")]
    [SerializeField] private float fadeSeconds = .25f;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (group != null)
        {
            group.alpha = 0f;
        }
    }

    public void Show(string text, float seconds, string speaker = "")
    {
        if (promptText!= null)
        {
            promptText.text = text;
        }

        if (speakerText != null)
        {
            speakerText.text = speaker;
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        }

        stopRoutine();
        showRoutine = StartCoroutine(ShowForSeconds(seconds));
    }

    public void Hide()
    {
        stopRoutine();
        showRoutine = StartCoroutine(FadeAway());
    }

    private IEnumerator ShowForSeconds(float seconds)
    {
        yield return FadeTo(1f);
        yield return new WaitForSecondsRealtime(seconds);
        yield return FadeTo(0f);
        showRoutine = null;
    }

    private IEnumerator FadeAway()
    {
        yield return FadeTo(0f);
        showRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (group == null)
        {
            yield break;
        }

        float startAlpha = group.alpha;
        float elapsed = 0f;

        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed/fadeSeconds);
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private void stopRoutine()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
    }
}

