using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialText : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlay;
    [Tooltip("Optional: assign the TextMeshPro UGUI component directly (overrides searching the overlay).")]
    [SerializeField] private TMP_Text overlayText;
    [Tooltip("Used only if the assigned TMP component's text is empty.")]

    [Header("Player detection")]
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (overlay == null && overlayText == null)
        {
            Debug.LogWarning($"{nameof(TutorialText)}: Neither {nameof(overlay)} nor {nameof(overlayText)} is assigned on '{gameObject.name}'.");
            return;
        }

        // If TMP_Text not assigned directly, try to find one under the overlay (including inactive).
        if (overlayText == null && overlay != null)
        {
            overlayText = overlay.GetComponentInChildren<TMP_Text>(true);
        }

        if (overlayText != null)
        {
            // Prevent unwanted wrapping/clipping and set default alignment (doesn't change RectTransform position).
            overlayText.overflowMode = TextOverflowModes.Overflow;
            overlayText.enableWordWrapping = false;
            overlayText.alignment = TextAlignmentOptions.MidlineLeft;

            // Force layout update so preferredWidth is accurate, then expand text rect as needed.
            Canvas.ForceUpdateCanvases();
            var rt = overlayText.rectTransform;

            // Force rebuild to ensure layout components (if any) have updated sizes.
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            const float padding = 20f;
            float preferredWidth = overlayText.preferredWidth;

            // Ensure the text RectTransform is wide enough to show the full message.
            if (rt.rect.width < preferredWidth + padding)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth + padding);
            }

            // If an overlay parent exists and is smaller than the text, expand it so the text is not clipped by parent rect.
            if (overlay != null)
            {
                var overlayRt = overlay.GetComponent<RectTransform>();
                if (overlayRt != null && overlayRt.rect.width < preferredWidth + padding)
                {
                    overlayRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth + padding);
                    Debug.Log($"{nameof(TutorialText)}: expanded overlay width to {preferredWidth + padding} to fit text.");
                }
            }

            // Do NOT change anchors/pivot/anchoredPosition here — position the text manually in the Unity editor.
            // Hide the text GameObject at start if an overlay GameObject provided.
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
            else
            {
                overlayText.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning($"{nameof(TutorialText)}: No TMPro.TMP_Text found in overlay '{overlay?.name ?? "null"}'.");
            if (overlay != null) overlay.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (overlay != null)
        {
            overlay.SetActive(true);
            return;
        }

        if (overlayText != null)
        {
            overlayText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        if (overlay != null)
        {
            overlay.SetActive(false);
            return;
        }

        if (overlayText != null)
        {
            overlayText.gameObject.SetActive(false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other != null && other.CompareTag(playerTag);
    }

    private void OnDisable()
    {
        if (overlay != null) overlay.SetActive(false);
        if (overlayText != null) overlayText.gameObject.SetActive(false);
    }
}