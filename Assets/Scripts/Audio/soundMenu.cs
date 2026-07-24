using UnityEngine;
using UnityEngine.UI;

public class SoundMenu : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    
    //[SerializeField] private Button muteButton;
    //[SerializeField] private Sprite soundOnSprite;
    //[SerializeField] private Sprite soundOffSprite;

    void Start()
    {
        if (audioManager.instance != null)
        {
            masterSlider.value = audioManager.instance.masterVolume;
            musicSlider.value = audioManager.instance.musicVolume;
            sfxSlider.value = audioManager.instance.sfxVolume;

            masterSlider.onValueChanged.AddListener(audioManager.instance.setMasterVolume);
            musicSlider.onValueChanged.AddListener(audioManager.instance.setMusicVolume);
            sfxSlider.onValueChanged.AddListener(audioManager.instance.setSFXVolume);
        }
    }
}