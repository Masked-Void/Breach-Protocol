using UnityEngine;

public class audioManager : MonoBehaviour
{
    public static audioManager instance { get; private set; }

    [Header("Audio Source")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("SFX")]
    [SerializeField] public AudioClip jump;
    [Range(0, 1)][SerializeField] public float jumpVol = .3f;

    [SerializeField] public AudioClip hurt;
    [Range(0, 1)][SerializeField] public float hurtVol = .3f;
    
    [SerializeField] public AudioClip steps;
    [Range(0, 1)][SerializeField] public float stepsVol = .3f;

    [SerializeField] public AudioClip enemySteps;
    [Range(0, 1)][SerializeField] public float enemyStepsVol = .3f;

    [SerializeField] public AudioClip enemyHit;
    [Range(0, 1)][SerializeField] public float enemyHitVol = .8f;
    
    [SerializeField] public AudioClip enemyShoot;
    [Range(0, 1)][SerializeField] public float enemyShootVol = .8f;
    
    [SerializeField] public AudioClip wallHit;
    [Range(0, 1)][SerializeField] public float wallHitVol = .8f;
    
    [SerializeField] public AudioClip equip;
    [Range(0, 1)][SerializeField] public float equipVol = .8f;

    [SerializeField] public AudioClip emptyMag;
    [Range(0, 1)][SerializeField] public float emptyMagVol = .8f;
    
    [SerializeField] public AudioClip bulletRicochet;
    [Range(0, 1)][SerializeField] public float bulletRicochetVol = .8f;
    
    [SerializeField] public AudioClip glass;
    [Range(0, 1)][SerializeField] public float glassVol = .8f;
    
    [SerializeField] public AudioClip buttonClick;
    [Range(0, 1)][SerializeField] public float buttonClickVol = .8f;
    
    [SerializeField] private AudioClip nukeSFX;
    [Range(0, 1)][SerializeField] public float nukeSFXVol = .8f;

    [Header("Music")]
    [SerializeField] public AudioClip titleScreenSound;
    [SerializeField] private AudioClip roundTransitionMusic;

    public bool isMuted;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource.spatialBlend = 0f;
    }

    public void loadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

        updateAudioVolumes();
    }

    public void saveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void updateAudioVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = isMuted ? 0f : (musicVolume * masterVolume);
        }
        if (sfxSource != null)
        {
            sfxSource.volume = isMuted ? 0f : (sfxVolume * masterVolume);
        }
    }

    public void setMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        updateAudioVolumes();
        saveSettings();
    }

    public void setMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        updateAudioVolumes();
        saveSettings();
    }

    public void setSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        updateAudioVolumes();
        saveSettings();
    }

    public void toggleMute()
    {
        isMuted = !isMuted;
        updateAudioVolumes();
        saveSettings();
    }

    public void playSFX(AudioClip clip, float localVolumeMod = 1f)
    {
        if (clip == null || sfxSource == null) return;

        float Volume = localVolumeMod * sfxVolume * masterVolume;
        sfxSource.PlayOneShot(clip, Volume);
    }

    public void playSpatialSFX(AudioClip clip, Vector3 position, float localVolumeMod = 1f, float minDistance = 1f, float maxDistance = 50f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("TempAudio");
        tempGO.transform.position = position;

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = localVolumeMod * sfxVolume * masterVolume;

        // 3D Spatial Audio setup
        source.spatialBlend = 1f; // Full 3D spatialization
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        source.Play();

        Destroy(tempGO, clip.length);
    }

    public void playMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume * masterVolume;
        musicSource.Play();
    }

    public void pauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void resumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    public void stopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void unmute()
    {
        isMuted = false;
        updateAudioVolumes();
        saveSettings();
    }

    public void playJump() => playSFX(jump, jumpVol);
    public void playHurt() => playSFX(hurt, hurtVol);
    public void playSteps() => playSFX(steps, stepsVol);
    public void playEquip() => playSFX(equip, equipVol);
    public void playEmptyMag() => playSFX(emptyMag, emptyMagVol);
    public void playButtonClick() => playSFX(buttonClick, buttonClickVol);
    public void playNuke() => playSFX(nukeSFX, nukeSFXVol);
    public void playTitleScreenSound() => playMusic(titleScreenSound);
    public void playRoundTransitionMusic()
    {
        stopMusic();
        playMusic(roundTransitionMusic);
    }
}