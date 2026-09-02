using System.Collections;
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
    [SerializeField] AudioClip[] jump;
    [Range(0, 1)][SerializeField] float jumpVol = .3f;

    [SerializeField] AudioClip[] hurt;
    [Range(0, 1)][SerializeField] float hurtVol = .3f;

    [SerializeField] AudioClip[] steps;
    [Range(0, 1)][SerializeField] float stepsVol = .3f;

    [SerializeField] public AudioClip[] enemySteps;
    [Range(0, 1)][SerializeField] public float enemyStepsVol = .3f;

    [SerializeField] public AudioClip[] enemyHit;
    [Range(0, 1)][SerializeField] public float enemyHitVol = .8f;

    [SerializeField] public AudioClip[] enemyShoot;
    [Range(0, 1)][SerializeField] public float enemyShootVol = .8f;

    [SerializeField] public AudioClip[] wallHit;
    [Range(0, 1)][SerializeField] public float wallHitVol = .8f;

    [SerializeField] public AudioClip[] equip;
    [Range(0, 1)][SerializeField] public float equipVol = .8f;

    [SerializeField] public AudioClip[] emptyMag;
    [Range(0, 1)][SerializeField] public float emptyMagVol = .8f;

    [SerializeField] public AudioClip[] bulletRicochet;
    [Range(0, 1)][SerializeField] public float bulletRicochetVol = .8f;

    [SerializeField] public AudioClip[] glass;
    [Range(0, 1)][SerializeField] public float glassVol = .8f;

    [SerializeField] public AudioClip[] explosion;
    [Range(0, 1)][SerializeField] public float explosionVol = .8f;

    [SerializeField] public AudioClip[] buttonClick;
    [Range(0, 1)][SerializeField] public float buttonClickVol = .8f;

    [SerializeField] AudioClip[] nukeSFX;
    [Range(0, 1)][SerializeField] public float nukeSFXVol = .8f;

    [SerializeField] public AudioClip[] electricSFX;
    [Range(0, 1)][SerializeField] public float electricSFXVol = .8f;

    [Header("Music")]
    [SerializeField] public AudioClip titleScreenSound;
    [SerializeField] public AudioClip pauseMenuMusic;
    [SerializeField] public AudioClip loseMenuMusic;
    [SerializeField] private AudioClip roundTransitionMusic;

    public bool isMuted = false;
    AudioClip savedGameplayClip;
    float savedGameplayTime;
    private Coroutine pauseMusicRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        sfxSource.spatialBlend = 0f;
        loadSettings();
    }

    public AudioClip pickRandomAudio(AudioClip[] audioList) => audioList[Random.Range(0, audioList.Length)];

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
            musicSource.volume = isMuted ? 0f : (musicVolume * masterVolume);
        if (sfxSource != null)
            sfxSource.volume = isMuted ? 0f : (sfxVolume * masterVolume);
    }

    private void OnDestroy() {
        if (instance == this)
            instance = null;
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
        stopPauseCoroutine();
        if (clip == null || musicSource == null) return;

        musicSource.clip = clip;
        musicSource.volume = isMuted ? 0f : musicVolume * masterVolume;
        musicSource.Play();
    }

    public void pauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying) musicSource.Pause();
    }

    public void resumeMusic()
    {
        if (musicSource != null) musicSource.UnPause();
    }

    public void stopMusic()
    {
        stopPauseCoroutine();
        if (musicSource != null) musicSource.Stop();
    }

    public void unmute()
    {
        isMuted = false;
        updateAudioVolumes();
        saveSettings();
    }

    public void playJump() => playSFX(pickRandomAudio(jump), jumpVol);
    public void playHurt() => playSFX(pickRandomAudio(hurt), hurtVol);
    public void playSteps() => playSFX(pickRandomAudio(steps), stepsVol);
    public void playEquip() => playSFX(pickRandomAudio(equip), equipVol);
    public void playEmptyMag() => playSFX(pickRandomAudio(emptyMag), emptyMagVol);
    public void playButtonClick() => playSFX(pickRandomAudio(buttonClick), buttonClickVol);
    public void playNuke() => playSFX(pickRandomAudio(nukeSFX), nukeSFXVol);
    public void playTitleScreenSound() => playMusic(titleScreenSound);
    public void playPauseMenuMusic()
    {
        if (musicSource != null && musicSource.clip != pauseMenuMusic)
        {
            savedGameplayClip = musicSource.clip;
            savedGameplayTime = musicSource.time;
        }
        playMusic(pauseMenuMusic);
    }

    public void restoreGameplayMusic()
    {
        stopPauseCoroutine();
        if (musicSource == null) return;
        if (musicSource.clip != pauseMenuMusic)
        {
            resumeMusic();
            updateAudioVolumes();
            return;
        }
        if (savedGameplayClip != null)
        {
            musicSource.clip = savedGameplayClip;
            musicSource.time = savedGameplayTime;
            musicSource.volume = isMuted ? 0f : (musicVolume * masterVolume);
            musicSource.Play();
        }
        else
        {
            stopMusic();
        }
    }

    public void playPauseMenuMusicWithDelay(float delaySeconds)
    {
        stopPauseCoroutine();
        pauseMusicRoutine = StartCoroutine(PlayPauseMusicDelayed(delaySeconds));
    }

    private IEnumerator PlayPauseMusicDelayed(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        playPauseMenuMusic();
        pauseMusicRoutine = null;
    }

    public void stopPauseCoroutine()
    {
        if (pauseMusicRoutine != null)
        {
            StopCoroutine(pauseMusicRoutine);
            pauseMusicRoutine = null;
        }
    }
    public void playLoseMenuMusic() { stopMusic(); playMusic(loseMenuMusic); }
    public void playRoundTransitionMusic() { stopMusic(); playMusic(roundTransitionMusic); }
}