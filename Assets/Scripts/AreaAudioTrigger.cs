using UnityEngine;
using System.Collections;

public class AreaAudioTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip musicToPlay;
    public AudioClip ambienceToPlay;
    
    [Header("Trigger Settings")]
    public bool playOnStart = false;
    public bool playOnTriggerEnter = true;
    public string playerTag = "Player";
    public float fadeDuration = 1.0f;

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(PlayAudioDelayed());
        }
    }

    private IEnumerator PlayAudioDelayed()
    {
        // Wait a frame to ensure GlobalAudioManager is initialized
        yield return null;
        if (GlobalAudioManager.Instance != null)
        {
            if (musicToPlay != null)
                GlobalAudioManager.Instance.PlayMusic(musicToPlay, fadeDuration);
            
            if (ambienceToPlay != null)
                GlobalAudioManager.Instance.PlayAmbience(ambienceToPlay, fadeDuration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnTriggerEnter && other.CompareTag(playerTag))
        {
            if (GlobalAudioManager.Instance != null)
            {
                if (musicToPlay != null)
                    GlobalAudioManager.Instance.PlayMusic(musicToPlay, fadeDuration);
                
                if (ambienceToPlay != null)
                    GlobalAudioManager.Instance.PlayAmbience(ambienceToPlay, fadeDuration);
            }
        }
    }
}
