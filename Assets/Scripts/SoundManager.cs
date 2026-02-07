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
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = true;
            musicSource.loop = true;
        }
    }

    public void PlaySpawn() => PlaySFX(spawnClip);
    public void PlayDrop() => PlaySFX(dropClip);
    public void PlayMerge() => PlaySFX(mergeClip);
    public void PlayGameOver() => PlaySFX(gameOverClip);
    public void PlayClick() => PlaySFX(clickClip);

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            // Fallback for debugging if clips aren't assigned yet
            Debug.Log($"Playing SFX: {(clip != null ? clip.name : "Unassigned Clip")}");
        }
    }
}
