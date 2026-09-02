using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundMenu : MonoBehaviour
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
        if (AudioManager.instance == null) return;

        AudioManager.instance.ToggleMute();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (AudioManager.instance == null) return;

        bool isMuted = AudioManager.instance.isMuted;

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(isMuted ? 0f : AudioManager.instance.masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(isMuted ? 0f : AudioManager.instance.musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(isMuted ? 0f : AudioManager.instance.sfxVolume);

        if (muteText != null)
        {
            muteText.text = isMuted ? "Unmute" : "Mute";
        }
    }

    private void onMasterSliderChanged(float val)
    {
        if (AudioManager.instance == null) return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetMasterVolume(val);
        UpdateVisuals();
    }

    private void onMusicSliderChanged(float val)
    {
        if (AudioManager.instance == null) return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetMusicVolume(val);
        UpdateVisuals();
    }

    private void onSFXSliderChanged(float val)
    {
        if (AudioManager.instance == null) return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetSFXVolume(val);
        UpdateVisuals();
    }
}