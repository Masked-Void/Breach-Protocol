using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class soundMenu : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private TextMeshProUGUI muteText;

    private void Awake()
    {
        if (masterSlider != null)
            masterSlider.onValueChanged.AddListener(onMasterSliderChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(onMusicSliderChanged);

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(onSFXSliderChanged);
    }

    private void OnEnable()
    {
        UpdateVisuals();
    }

    public void ToggleMute()
    {
        if (audioManager.instance == null) return;

        audioManager.instance.toggleMute();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (audioManager.instance == null) return;

        bool isMuted = audioManager.instance.isMuted;

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(isMuted ? 0f : audioManager.instance.masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(isMuted ? 0f : audioManager.instance.musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(isMuted ? 0f : audioManager.instance.sfxVolume);

        if (muteText != null)
        {
            muteText.text = isMuted ? "Unmute" : "Mute";
        }
    }

    private void onMasterSliderChanged(float val)
    {
        if (audioManager.instance == null) return;

        if (audioManager.instance.isMuted && val > 0f)
        {
            audioManager.instance.unmute();
        }

        audioManager.instance.setMasterVolume(val);
        UpdateVisuals();
    }

    private void onMusicSliderChanged(float val)
    {
        if (audioManager.instance == null) return;

        if (audioManager.instance.isMuted && val > 0f)
        {
            audioManager.instance.unmute();
        }

        audioManager.instance.setMusicVolume(val);
        UpdateVisuals();
    }

    private void onSFXSliderChanged(float val)
    {
        if (audioManager.instance == null) return;

        if (audioManager.instance.isMuted && val > 0f)
        {
            audioManager.instance.unmute();
        }

        audioManager.instance.setSFXVolume(val);
        UpdateVisuals();
    }
}