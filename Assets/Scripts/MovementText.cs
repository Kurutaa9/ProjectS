using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MovementText : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlay;
    [Tooltip("Optional: assign the UI Text component directly (overrides searching the overlay).")]
    [SerializeField] private Text overlayText;
    [Tooltip("If a UnityEngine.UI.Text is found, this message will be applied.")]
    [SerializeField] private string message = "Use WASD to walk. Hold Shift to run.";
    [Tooltip("Horizontal offset in pixels from the left edge when positioned at middle-left.")]
    [SerializeField] private float horizontalOffset = 50f;

    [Header("Player detection")]
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        if (overlay == null && overlayText == null)
        {
            Debug.LogWarning($"{nameof(MovementText)}: Neither {nameof(overlay)} nor {nameof(overlayText)} is assigned on '{gameObject.name}'.");
            return;
        }

        // If Text not assigned directly, try to find one under the overlay (including inactive).
        if (overlayText == null && overlay != null)
        {
            overlayText = overlay.GetComponentInChildren<Text>(true);
        }

        if (overlayText != null)
        {
            overlayText.text = message;

            // Make sure the Text won't wrap/truncate unexpectedly and aligns left-middle.
            overlayText.horizontalOverflow = HorizontalWrapMode.Overflow;
            overlayText.verticalOverflow = VerticalWrapMode.Overflow;
            overlayText.alignment = TextAnchor.MiddleLeft;

            // Force layout update so preferredWidth is accurate, then expand rects as needed.
            Canvas.ForceUpdateCanvases();
            var rt = overlayText.rectTransform;

            // Force rebuild to ensure layout components (if any) have updated sizes.
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            const float padding = 20f;
            float preferredWidth = overlayText.preferredWidth;
            float requiredWidth = Mathf.Abs(horizontalOffset) + preferredWidth + padding;

            // Ensure the text RectTransform is wide enough to show the full message.
            float currentTextWidth = rt.rect.width;
            if (currentTextWidth < preferredWidth + padding)
            {
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, preferredWidth + padding);
            }

            // If an overlay parent exists and is smaller than the required width, expand it so the text is not clipped by parent rect.
            if (overlay != null)
            {
                var overlayRt = overlay.GetComponent<RectTransform>();
                if (overlayRt != null && overlayRt.rect.width < requiredWidth)
                {
                    overlayRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, requiredWidth);
                    Debug.Log($"{nameof(MovementText)}: expanded overlay width to {requiredWidth} to fit text.");
                }
            }

            PositionTextMiddleLeft(overlayText, horizontalOffset);

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
            Debug.LogWarning($"{nameof(MovementText)}: No UnityEngine.UI.Text found in overlay '{overlay?.name ?? "null"}'.");
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

    // Position the UI Text RectTransform to the middle-left of the parent rect.
    private void PositionTextMiddleLeft(Text uiText, float offsetFromLeft)
    {
        if (uiText == null) return;
        var rt = uiText.rectTransform;
        if (rt == null) return;

        // Anchor to the left center of the parent canvas / parent rect
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);

        // Pivot left-center so anchoredPosition.x is distance from left edge
        rt.pivot = new Vector2(0f, 0.5f);

        // Horizontal offset in pixels from the left; vertical centered (0)
        rt.anchoredPosition = new Vector2(Mathf.Abs(offsetFromLeft), 0f);
    }
}
