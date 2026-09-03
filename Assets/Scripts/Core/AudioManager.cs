using System.Collections;
using UnityEngine;

/*
 * Script: AudioManager
 *
 * Description:
 * One place for every sound in the game. Holds the clip banks, the three
 * volume sliders, and the music state. Lives in Bootstrap so music survives
 * level loads.
 *
 * Responsibilities:
 * - Play one-shot SFX, and spatial SFX at a world position
 * - Play, pause and restore music, including the pause menu swap
 * - Own master, music and SFX volume, saved to PlayerPrefs
 *
 * Interacts With:
 * - SoundMenu (volume sliders)
 * - GameManager (pause and lose music)
 * - Almost every gameplay script (fires one-shots)
 *
 * Notes:
 * - Clip fields are arrays so each sound can have variations. PickRandomAudio
 *   chooses one, which stops repeated sounds getting grating.
 * - PlaySpatialSFX creates a temporary GameObject per sound and destroys it
 *   when the clip ends. Fine at current volume, would need pooling if it
 *   fired every frame.
 */
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance { get; private set; }

    [Header("Audio Source")]
    [Tooltip("plays every one shot sound effect, set to 2D")]
    public AudioSource sfxSource;

    [Tooltip("plays looping music, only one track at a time")]
    public AudioSource musicSource;

    [Header("Volume Settings")]
    [Tooltip("scales both music and sfx, saved to playerprefs")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Tooltip("music volume before the master multiplier")]
    [Range(0f, 1f)] public float musicVolume = 1f;

    [Tooltip("sfx volume before the master multiplier")]
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("SFX — Player")]
    [Tooltip("variations played when the player jumps")]
    [SerializeField] AudioClip[] jump;
    [Range(0, 1)][SerializeField] float jumpVol = .3f;

    [Tooltip("variations played when the player takes damage")]
    [SerializeField] AudioClip[] hurt;
    [Range(0, 1)][SerializeField] float hurtVol = .3f;

    [Tooltip("footstep variations, fired on an interval while moving")]
    [SerializeField] AudioClip[] steps;
    [Range(0, 1)][SerializeField] float stepsVol = .3f;

    [Header("SFX — Enemies")]
    [Tooltip("enemy footsteps, fired on an interval while they move")]
    [SerializeField] public AudioClip[] enemySteps;
    [Range(0, 1)][SerializeField] public float enemyStepsVol = .3f;

    [Tooltip("played when a shot connects with an enemy")]
    [SerializeField] public AudioClip[] enemyHit;
    [Range(0, 1)][SerializeField] public float enemyHitVol = .8f;

    [Tooltip("played when a ranged enemy fires")]
    [SerializeField] public AudioClip[] enemyShoot;
    [Range(0, 1)][SerializeField] public float enemyShootVol = .8f;

    [Header("SFX — Impacts")]
    [Tooltip("played when a shot hits level geometry")]
    [SerializeField] public AudioClip[] wallHit;
    [Range(0, 1)][SerializeField] public float wallHitVol = .8f;

    [Tooltip("played when a shot glances off metal")]
    [SerializeField] public AudioClip[] bulletRicochet;
    [Range(0, 1)][SerializeField] public float bulletRicochetVol = .8f;

    [Tooltip("played when breakable glass shatters")]
    [SerializeField] public AudioClip[] glass;
    [Range(0, 1)][SerializeField] public float glassVol = .8f;

    [Tooltip("played on any explosion, including destructibles")]
    [SerializeField] public AudioClip[] explosion;
    [Range(0, 1)][SerializeField] public float explosionVol = .8f;

    [Header("SFX — Weapons")]
    [Tooltip("played when the player picks up or swaps a weapon")]
    [SerializeField] public AudioClip[] equip;
    [Range(0, 1)][SerializeField] public float equipVol = .8f;

    [Tooltip("dry fire click when the player is out of ammo")]
    [SerializeField] public AudioClip[] emptyMag;
    [Range(0, 1)][SerializeField] public float emptyMagVol = .8f;

    [Header("SFX — Scorestreaks")]
    [Tooltip("played when the nuke scorestreak fires")]
    [SerializeField] AudioClip[] nukeSFX;
    [Range(0, 1)][SerializeField] public float nukeSFXVol = .8f;

    [Tooltip("electric sting used by ddos and overclock")]
    [SerializeField] public AudioClip[] electricSFX;
    [Range(0, 1)][SerializeField] public float electricSFXVol = .8f;

    [Header("SFX — UI")]
    [Tooltip("played on any menu button press")]
    [SerializeField] public AudioClip[] buttonClick;
    [Range(0, 1)][SerializeField] public float buttonClickVol = .8f;

    [Header("Music")]
    [Tooltip("plays on the title screen")]
    [SerializeField] public AudioClip titleScreenSound;

    [Tooltip("swaps in when the player pauses, gameplay track resumes after")]
    [SerializeField] public AudioClip pauseMenuMusic;

    [Tooltip("plays on the lose screen")]
    [SerializeField] public AudioClip loseMenuMusic;

    [Tooltip("plays during the gap between waves")]
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
        LoadSettings();
    }

    public AudioClip PickRandomAudio(AudioClip[] audioList) => audioList[Random.Range(0, audioList.Length)];

    public void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        isMuted = PlayerPrefs.GetInt("IsMuted", 0) == 1;

        UpdateAudioVolumes();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetInt("IsMuted", isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void UpdateAudioVolumes()
    {
        if (musicSource != null)
            musicSource.volume = isMuted ? 0f : (musicVolume * masterVolume);
        if (sfxSource != null)
            sfxSource.volume = isMuted ? 0f : (sfxVolume * masterVolume);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void SetMasterVolume(float vol)
    {
        masterVolume = Mathf.Clamp01(vol);
        UpdateAudioVolumes();
        SaveSettings();
    }

    public void SetMusicVolume(float vol)
    {
        musicVolume = Mathf.Clamp01(vol);
        UpdateAudioVolumes();
        SaveSettings();
    }

    public void SetSFXVolume(float vol)
    {
        sfxVolume = Mathf.Clamp01(vol);
        UpdateAudioVolumes();
        SaveSettings();
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        UpdateAudioVolumes();
        SaveSettings();
    }

    public void PlaySFX(AudioClip clip, float localVolumeMod = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        float Volume = localVolumeMod * sfxVolume * masterVolume;
        sfxSource.PlayOneShot(clip, Volume);
    }

    public void PlaySpatialSFX(AudioClip clip, Vector3 position, float localVolumeMod = 1f, float minDistance = 1f, float maxDistance = 50f)
    {
        if (clip == null)
            return;

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

    public void PlayMusic(AudioClip clip)
    {
        StopPauseCoroutine();
        if (clip == null || musicSource == null)
            return;

        musicSource.clip = clip;
        musicSource.volume = isMuted ? 0f : musicVolume * masterVolume;
        musicSource.Play();
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (musicSource != null)
            musicSource.UnPause();
    }

    public void StopMusic()
    {
        StopPauseCoroutine();
        if (musicSource != null)
            musicSource.Stop();
    }

    public void Unmute()
    {
        isMuted = false;
        UpdateAudioVolumes();
        SaveSettings();
    }

    public void PlayJump() => PlaySFX(PickRandomAudio(jump), jumpVol);
    public void PlayHurt() => PlaySFX(PickRandomAudio(hurt), hurtVol);
    public void PlaySteps() => PlaySFX(PickRandomAudio(steps), stepsVol);
    public void PlayEquip() => PlaySFX(PickRandomAudio(equip), equipVol);
    public void PlayEmptyMag() => PlaySFX(PickRandomAudio(emptyMag), emptyMagVol);
    public void PlayButtonClick() => PlaySFX(PickRandomAudio(buttonClick), buttonClickVol);
    public void PlayNuke() => PlaySFX(PickRandomAudio(nukeSFX), nukeSFXVol);
    public void PlayTitleScreenSound() => PlayMusic(titleScreenSound);
    public void PlayPauseMenuMusic()
    {
        if (musicSource != null && musicSource.clip != pauseMenuMusic)
        {
            savedGameplayClip = musicSource.clip;
            savedGameplayTime = musicSource.time;
        }
        PlayMusic(pauseMenuMusic);
    }

    public void RestoreGameplayMusic()
    {
        StopPauseCoroutine();
        if (musicSource == null)
            return;
        if (musicSource.clip != pauseMenuMusic)
        {
            ResumeMusic();
            UpdateAudioVolumes();
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
            StopMusic();
        }
    }

    public void PlayPauseMenuMusicWithDelay(float delaySeconds)
    {
        StopPauseCoroutine();
        pauseMusicRoutine = StartCoroutine(PlayPauseMusicDelayed(delaySeconds));
    }

    private IEnumerator PlayPauseMusicDelayed(float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        PlayPauseMenuMusic();
        pauseMusicRoutine = null;
    }

    public void StopPauseCoroutine()
    {
        if (pauseMusicRoutine != null)
        {
            StopCoroutine(pauseMusicRoutine);
            pauseMusicRoutine = null;
        }
    }
    public void PlayLoseMenuMusic() { StopMusic(); PlayMusic(loseMenuMusic); }
    public void PlayRoundTransitionMusic() { StopMusic(); PlayMusic(roundTransitionMusic); }
}