using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("SFX Clips")]
    public AudioClip spawnClip;
    public AudioClip dropClip;
    public AudioClip mergeClip;
    public AudioClip gameOverClip;
    public AudioClip clickClip;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
            Debug.Log($"<color=cyan>SoundManager initialized on {gameObject.name}</color>");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0; // Force 2D for reliability
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = true;
            musicSource.loop = true;
            musicSource.spatialBlend = 0;
        }
    }

    // Call this on first click to "wake up" audio on mobile/web browsers
    public void ResumeAudioContext()
    {
        // Removed the dspTime < 0.1f check which was likely blocking the wake-up
        Debug.Log("<color=cyan>[SOUND] Attempting to wake up AudioContext...</color>");
        
        // Ensure AudioSource is enabled
        if (sfxSource != null) 
        {
            sfxSource.enabled = true;
            if (clickClip != null) sfxSource.PlayOneShot(clickClip); // Force a noise to wake it
        }
    }

    public void PlaySpawn() => PlaySFX(spawnClip, "Spawn");
    public void PlayDrop() => PlaySFX(dropClip, "Drop");
    public void PlayMerge() => PlaySFX(mergeClip, "Merge");
    public void PlayGameOver() => PlaySFX(gameOverClip, "GameOver");
    public void PlayClick() => PlaySFX(clickClip, "Click");

    private void PlaySFX(AudioClip clip, string actionName)
    {
        if (clip != null && sfxSource != null)
        {
            Debug.Log($"<color=green>[SOUND] Playing {actionName}: {clip.name}</color>");
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"<color=red>[SOUND ERROR] Cannot play {actionName}! Clip={clip}, Source={sfxSource}</color>");
        }
    }
}
