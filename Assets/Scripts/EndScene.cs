using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScene : MonoBehaviour
{
    [Header("Settings")]
    public float fadeDuration = 2.0f;
    public float creditsStartDelay = 4.0f; // Time from trigger to credits start
    public float scrollSpeed = 50f;

    [Header("UI References")]
    public Image whiteFadeImage; // Assign a white UI Image that stretches across the screen
    public RectTransform creditsRect; // Assign the RectTransform of the credits text/container

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip endAudioClip;

    private bool isTriggered = false;
    private bool isScrolling = false;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isScrolling && creditsRect != null)
        {
            creditsRect.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        // Check if the object entering is the player
        // Make sure your Player object has the tag "Player"
        if (other.CompareTag("Player"))
        {
            isTriggered = true;
            PlayerController pc = other.GetComponent<PlayerController>();
            StartCoroutine(ShowCreditsSequence(pc));
        }
    }

    private IEnumerator ShowCreditsSequence(PlayerController pc)
    {
        if (whiteFadeImage != null)
        {
            whiteFadeImage.gameObject.SetActive(true);

            // Force the image to stretch to fill the entire screen/canvas
            RectTransform rt = whiteFadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; // Sets offsetMin and offsetMax to (0,0) effectively
            rt.anchoredPosition = Vector2.zero;
            
            // Reset the base color to black for fade out
            whiteFadeImage.color = Color.black;

            whiteFadeImage.canvasRenderer.SetAlpha(0.0f);
            whiteFadeImage.CrossFadeAlpha(1.0f, fadeDuration, false);
        }

        // Wait 0.5 second then lock inputs
        yield return new WaitForSeconds(0.5f);
        if (pc != null) pc.inputsLocked = true;

        // Wait until 2.0 seconds total have passed (2.0 - 0.5 = 1.5 seconds more)
        yield return new WaitForSeconds(1.5f);
        
        // Play Audio
        if (audioSource != null && endAudioClip != null)
        {
            audioSource.PlayOneShot(endAudioClip);
        }

        // Wait until 2.5 seconds total have passed (2.5 - 2.0 = 0.5 seconds more)
        yield return new WaitForSeconds(0.5f);

        // Start scrolling the credits immediately (at 4 seconds)
        if (creditsRect != null)
        {
            // FIX: Ensure the text is drawn ON TOP of the black background
            // If the background is on a high-sorting-order canvas (like from TtMTP),
            // we need to move the text to that same canvas or it will be hidden.
            if (whiteFadeImage != null && whiteFadeImage.transform.parent != null)
            {
                // Move credits to the same canvas as the background
                creditsRect.SetParent(whiteFadeImage.transform.parent, true);
            }

            // Bring to front
            creditsRect.SetAsLastSibling();
            
            creditsRect.gameObject.SetActive(true);
            isScrolling = true;
        }
    }
}
