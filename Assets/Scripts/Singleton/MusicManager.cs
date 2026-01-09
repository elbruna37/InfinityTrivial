using UnityEngine;
using System.Collections;
using DG.Tweening;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Music Lists")]
    public AudioClip[] menuMusic;
    public AudioClip[] backgroundMusic;
    public AudioClip[] stealMusic;

    private AudioSource audioSource;

    // Reproducción actual
    private AudioClip[] currentPlaylist;
    private string currentPlaylistName = "";
    private int currentIndex = 0;

    private Coroutine autoPlayCoroutine;

    // Slots de pausa
    private string pausedPlaylist1 = "";
    private string pausedPlaylist2 = "";

    private int pausedIndex1 = -1;
    private int pausedIndex2 = -1;

    private float pausedTime1 = -1f;
    private float pausedTime2 = -1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;
    }

    // -----------------------
    // PLAY MUSIC 
    // -----------------------
    public void PlayMusic(string playlistName)
    {
        Debug.Log($"Playlist '{playlistName}' run");
        StopAuto();

        currentPlaylist = GetPlaylist(playlistName);
        currentPlaylistName = playlistName;

        if (currentPlaylist == null || currentPlaylist.Length == 0)
        {
            Debug.LogWarning($"Playlist '{playlistName}' not found or empty.");
            return;
        }

        float volume = PlayerPrefs.GetFloat("volume", 1f);
        audioSource.volume = volume;

        currentIndex = Random.Range(0, currentPlaylist.Length);

        audioSource.clip = currentPlaylist[currentIndex];
        audioSource.time = 0f;
        audioSource.Play();

        autoPlayCoroutine = StartCoroutine(AutoPlay());
    }

    private IEnumerator AutoPlay()
    {
        while (true)
        {
            yield return new WaitUntil(() => !audioSource.isPlaying);

            if (currentPlaylist == null || currentPlaylist.Length == 0)
                yield break;

            currentIndex = Random.Range(0, currentPlaylist.Length);

            audioSource.clip = currentPlaylist[currentIndex];
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private AudioClip[] GetPlaylist(string name)
    {
        return name switch
        {
            "menuMusic" => menuMusic,
            "backgroundMusic" => backgroundMusic,
            "stealMusic" => stealMusic,
            _ => null,
        };
    }

    private void StopAuto()
    {
        if (autoPlayCoroutine != null)
        {
            StopCoroutine(autoPlayCoroutine);
            autoPlayCoroutine = null;
        }
    }

    // -----------------------
    // PAUSE (slot 1 o 2)
    // -----------------------
    public void PauseMusic(int slot, float fadeTime = 1f)
    {
        Debug.Log($"Pause Playlist in slot {slot}");

        if (!audioSource.isPlaying)
            return;

        StopAuto();

        float originalVolume = audioSource.volume;

        if (slot == 1)
        {
            pausedPlaylist1 = currentPlaylistName;
            pausedIndex1 = currentIndex;
            pausedTime1 = audioSource.time;
        }
        else if (slot == 2)
        {
            pausedPlaylist2 = currentPlaylistName;
            pausedIndex2 = currentIndex;
            pausedTime2 = audioSource.time;
        }
        else
        {
            Debug.LogWarning("PauseMusic slot must be 1 or 2");
            return;
        }

        StopAuto();
        audioSource.DOKill();
        audioSource.DOFade(0f, fadeTime).OnComplete(() =>
        {
            audioSource.Pause();
            audioSource.volume = originalVolume;
        });
    }

    // -----------------------
    // RESUME (slot 1 o 2)
    // -----------------------
    public void ResumeMusic(int slot, float fadeTime = 1f)
    {
        Debug.Log($"Resume Playlist in slot {slot}");

        string playlistName = "";
        int index = -1;
        float time = 0f;

        if (slot == 1)
        {
            playlistName = pausedPlaylist1;
            index = pausedIndex1;
            time = pausedTime1;
        }
        else if (slot == 2)
        {
            playlistName = pausedPlaylist2;
            index = pausedIndex2;
            time = pausedTime2;
        }
        else
        {
            Debug.LogWarning("ResumeMusic slot must be 1 or 2");
            return;
        }

        if (string.IsNullOrEmpty(playlistName))
        {
            Debug.LogWarning($"Slot {slot} is empty. Can't resume.");
            return;
        }

        currentPlaylist = GetPlaylist(playlistName);
        currentPlaylistName = playlistName;
        currentIndex = index;

        float targetVolume = PlayerPrefs.GetFloat("volume", 1f);

        StopAuto();
        audioSource.DOKill();

        audioSource.clip = currentPlaylist[currentIndex];
        audioSource.time = time;
        audioSource.volume = 0f;

        audioSource.Play();
        audioSource.DOFade(targetVolume, fadeTime);

        autoPlayCoroutine = StartCoroutine(AutoPlay());
    }

    // -----------------------
    // STOP con fade-out
    // -----------------------
    public void StopMusic(float fadeTime = 1f)
    {
        StopAuto();
        float originalVolume = audioSource.volume;

        audioSource.DOFade(0f, fadeTime).OnComplete(() =>
        {
            audioSource.Stop();
            audioSource.volume = originalVolume;
        });
    }

}
