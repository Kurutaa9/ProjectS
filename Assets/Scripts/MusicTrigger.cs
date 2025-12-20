using UnityEngine;
using System.Collections;

public class MusicTrigger : MonoBehaviour
{
    public AudioClip musicToPlay;
    public bool playOnStart = true;
    public bool playOnTriggerEnter = false;
    public string playerTag = "Player";
    public float fadeDuration = 1.0f;

    private void Start()
    {
        if (playOnStart && musicToPlay != null)
        {
            StartCoroutine(PlayMusicDelayed());
        }
    }

    private IEnumerator PlayMusicDelayed()
    {
        // Wait a frame to ensure GlobalAudioManager is initialized
        yield return null;
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlayMusic(musicToPlay, fadeDuration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnTriggerEnter && other.CompareTag(playerTag) && musicToPlay != null)
        {
            if (GlobalAudioManager.Instance != null)
            {
                GlobalAudioManager.Instance.PlayMusic(musicToPlay, fadeDuration);
            }
        }
    }
}
