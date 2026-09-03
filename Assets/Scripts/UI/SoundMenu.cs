using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script: SoundMenu
 *
 * Description:
 * The three volume sliders and the mute toggle. Wires them to AudioManager in
 * code rather than through the inspector, so renaming a handler is caught by
 * the compiler.
 *
 * Interacts With:
 * - AudioManager (reads current values, writes changes, saves to PlayerPrefs)
 */
public class SoundMenu : MonoBehaviour
{
    [Tooltip("scales both music and sfx")]
    [SerializeField] private Slider masterSlider;

    [Tooltip("music only, before the master multiplier")]
    [SerializeField] private Slider musicSlider;

    [Tooltip("sound effects only, before the master multiplier")]
    [SerializeField] private Slider sfxSlider;

    [Tooltip("label on the mute button, swaps between Mute and Unmute")]
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
        if (AudioManager.instance == null)
            return;

        AudioManager.instance.ToggleMute();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (AudioManager.instance == null)
            return;

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
        if (AudioManager.instance == null)
            return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetMasterVolume(val);
        UpdateVisuals();
    }

    private void onMusicSliderChanged(float val)
    {
        if (AudioManager.instance == null)
            return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetMusicVolume(val);
        UpdateVisuals();
    }

    private void onSFXSliderChanged(float val)
    {
        if (AudioManager.instance == null)
            return;

        if (AudioManager.instance.isMuted && val > 0f)
        {
            AudioManager.instance.Unmute();
        }

        AudioManager.instance.SetSFXVolume(val);
        UpdateVisuals();
    }
}