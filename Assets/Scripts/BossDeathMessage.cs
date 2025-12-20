using UnityEngine;
using TMPro;
using System.Collections;

public class BossDeathMessage : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI messageText;
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    public string message = "FOE SLAUGHTERED";
    public float fadeInDuration = 0.5f;
    public float displayDuration = 3f;
    public float fadeOutDuration = 1f;
    public AudioClip deathSound;
    
    [Header("Animation")]
    public float scaleAmount = 1.2f;

    private AudioSource audioSource;

    void Start()
    {
        if (messageText != null)
            messageText.text = message;
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Start invisible
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // Play Sound
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound);

        // Fade In
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            if (canvasGroup) canvasGroup.alpha = timer / fadeInDuration;
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 1f;

        // Wait
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            if (canvasGroup) canvasGroup.alpha = 1f - (timer / fadeOutDuration);
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 0f;

        Destroy(gameObject);
    }
}
