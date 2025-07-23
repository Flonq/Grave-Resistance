using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambientSource;
    
    [Header("Music Clips")]
    public AudioClip backgroundMusic;
    
    [Header("SFX Clips")]
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    
    [Header("Ambient Clips")]
    public AudioClip windAmbient;
    public AudioClip chainAmbient;
    
    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.3f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;
    [Range(0f, 1f)]
    public float ambientVolume = 0.2f;
    
    // Singleton Pattern
    public static AudioManager Instance;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Audio Source'ları otomatik bul
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 3)
        {
            musicSource = sources[0];
            sfxSource = sources[1];
            ambientSource = sources[2];
        }
    }
    
    private void Start()
    {
        // Background music başlat
        PlayBackgroundMusic();
        
        // Ambient sound başlat
        PlayAmbientSound();
        
        // Volume ayarlarını yükle
        LoadVolumeSettings();
    }
    
    // BACKGROUND MUSIC
    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }
    
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }
    
    // SFX SOUNDS
    public void PlayButtonClick()
    {
        PlaySFX(buttonClickSound);
    }
    
    public void PlayButtonHover()
    {
        PlaySFX(buttonHoverSound);
    }
    
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }
    
    // AMBIENT SOUNDS
    public void PlayAmbientSound()
    {
        if (windAmbient != null && ambientSource != null)
        {
            ambientSource.clip = windAmbient;
            ambientSource.Play();
        }
    }
    
    // VOLUME CONTROL
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        UpdateAllVolumes();
        SaveVolumeSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        UpdateMusicVolume();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        UpdateSFXVolume();
    }
    
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = volume;
        UpdateAmbientVolume();
    }
    
    private void UpdateAllVolumes()
    {
        UpdateMusicVolume();
        UpdateSFXVolume();
        UpdateAmbientVolume();
    }
    
    private void UpdateMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume * masterVolume;
    }
    
    private void UpdateSFXVolume()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume * masterVolume;
    }
    
    private void UpdateAmbientVolume()
    {
        if (ambientSource != null)
            ambientSource.volume = ambientVolume * masterVolume;
    }
    
    // SETTINGS INTEGRATION
    public void OnMasterVolumeChanged(float value)
    {
        SetMasterVolume(value);
    }
    
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
        PlayerPrefs.Save();
    }
    
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.3f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
        ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 0.2f);
        
        UpdateAllVolumes();
    }
}
