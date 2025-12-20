using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance;

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource ambienceSource; // New Ambience Source
    public AudioSource sfxSource; // Optional global SFX source

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float ambienceVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Default Music")]
    public AudioClip defaultBGM;
    public AudioClip defaultAmbience;

    private bool isMusicFading = false;
    private bool isAmbienceFading = false;
    private Coroutine musicFadeCoroutine;
    private Coroutine ambienceFadeCoroutine;

    private void Awake()
    {
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

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (ambienceSource == null)
        {
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        if (defaultBGM != null)
        {
            PlayMusic(defaultBGM);
        }
        if (defaultAmbience != null)
        {
            PlayAmbience(defaultAmbience);
        }
    }

    private void Update()
    {
        // Update volumes in real-time (useful for settings menus)
        if (!isMusicFading && musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        
        if (!isAmbienceFading && ambienceSource != null)
        {
            ambienceSource.volume = ambienceVolume * masterVolume;
        }

        if (sfxSource != null) 
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1.0f)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, fadeDuration));
    }

    public void StopMusic(float fadeDuration = 1.0f)
    {
        if (musicFadeCoroutine != null) StopCoroutine(musicFadeCoroutine);
        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(null, fadeDuration));
    }

    public void PlayAmbience(AudioClip clip, float fadeDuration = 1.0f)
    {
        if (clip == null) return;
        if (ambienceSource.clip == clip && ambienceSource.isPlaying) return;

        if (ambienceFadeCoroutine != null) StopCoroutine(ambienceFadeCoroutine);
        ambienceFadeCoroutine = StartCoroutine(FadeAmbienceRoutine(clip, fadeDuration));
    }

    public void StopAmbience(float fadeDuration = 1.0f)
    {
        if (ambienceFadeCoroutine != null) StopCoroutine(ambienceFadeCoroutine);
        ambienceFadeCoroutine = StartCoroutine(FadeAmbienceRoutine(null, fadeDuration));
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
    {
        isMusicFading = true;
        float startVolume = musicSource.volume;
        
        // Fade out
        if (musicSource.isPlaying)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }
            musicSource.Stop();
        }

        if (newClip != null)
        {
            musicSource.clip = newClip;
            musicSource.Play();

            // Fade in
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float targetVolume = musicVolume * masterVolume;
                musicSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
                yield return null;
            }
        }
        isMusicFading = false;
    }

    private IEnumerator FadeAmbienceRoutine(AudioClip newClip, float duration)
    {
        isAmbienceFading = true;
        float startVolume = ambienceSource.volume;
        
        // Fade out
        if (ambienceSource.isPlaying)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                ambienceSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
                yield return null;
            }
            ambienceSource.Stop();
        }

        if (newClip != null)
        {
            ambienceSource.clip = newClip;
            ambienceSource.Play();

            // Fade in
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float targetVolume = ambienceVolume * masterVolume;
                ambienceSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
                yield return null;
            }
        }
        isAmbienceFading = false;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volumeScale * sfxVolume * masterVolume);
        }
    }
}
