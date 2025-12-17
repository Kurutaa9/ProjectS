using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TtMTP : MonoBehaviour
{
    [Tooltip("Optional Transform to move the player to. If null the Vector3 below is used.")]
    public Transform targetTransform;

    [Tooltip("Fallback position to teleport the player to (matches your screenshot).")]
    public Vector3 targetPosition = new Vector3(75f, 16.25f, 75f);

    [Tooltip("If true this script reacts to OnTriggerEnter. If false it reacts to OnCollisionEnter.")]
    public bool useTrigger = true;

    [Tooltip("Tag name used to identify the player GameObject.")]
    public string playerTag = "Player";

    [Tooltip("How long the player inputs stay locked after teleporting (seconds).")]
    public float lockDuration = 0.1f;

    [Header("Screen glow transition")]
    [Tooltip("Full-screen UI Image used as a white overlay. Assign a screen-sized Image (Color white) and set Raycast Target = false.")]
    public Image glowImage;

    [Tooltip("Total time of the glow transition in seconds (grow then fade).")]
    public float glowDuration = 0.6f;

    [Tooltip("Peak alpha for the glow overlay.")]
    [Range(0f, 1f)]
    public float glowPeakAlpha = 1f;

    [Tooltip("Optional curve for glow intensity over time. Null will use linear ease in/out.")]
    public AnimationCurve glowCurve;

    // Prevent re-entrant teleports / overlapping glow animations.
    private bool isTeleporting;

    // If this script creates an overlay Canvas it will store the reference here so it can configure it.
    private Canvas createdOverlayCanvas;

    // Sorting order used for the overlay canvas to guarantee it renders on top.
    private const int OverlaySortingOrder = 32767;

    private void OnValidate()
    {
        if (glowCurve == null)
        {
            // simple ease in/out if inspector left curve empty
            glowCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void Start()
    {
        // Ensure the overlay is hidden by default so it only appears during teleport.
        if (glowImage != null)
        {
            // Make sure the image does not block input
            glowImage.raycastTarget = false;

            // Move the image to its own topmost Canvas so it covers every other overlay.
            EnsureTopmostCanvasForGlow();

            // hide by default
            glowImage.gameObject.SetActive(false);
            var c = glowImage.color;
            c.a = 0f;
            glowImage.color = c;
        }
    }

    /// <summary>
    /// Ensures the assigned glowImage is parented under a Canvas that will always render on top of other UI.
    /// If a suitable created canvas already exists the image is reparented to it. If not, a new Canvas is created.
    /// </summary>
    private void EnsureTopmostCanvasForGlow()
    {
        if (glowImage == null) return;

        // If glowImage is already under a Canvas that already overrides sorting at a high order, prefer that.
        Canvas existing = glowImage.GetComponentInParent<Canvas>();
        if (existing != null && existing.overrideSorting && existing.sortingOrder >= OverlaySortingOrder)
        {
            // Already topmost enough; just make sure the image stretches full screen and won't block raycasts.
            RectTransform rtExist = glowImage.rectTransform;
            rtExist.anchorMin = Vector2.zero;
            rtExist.anchorMax = Vector2.one;
            rtExist.offsetMin = Vector2.zero;
            rtExist.offsetMax = Vector2.zero;
            glowImage.raycastTarget = false;
            return;
        }

        // If we previously created a canvas, reuse it
        if (createdOverlayCanvas == null)
        {
            GameObject canvasGO = new GameObject("GlowOverlayCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(null); // root-level so it isn't clipped by other UI
            createdOverlayCanvas = canvasGO.GetComponent<Canvas>();
            createdOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdOverlayCanvas.overrideSorting = true;
            createdOverlayCanvas.sortingOrder = OverlaySortingOrder;

            // Add recommended components for a Canvas
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // We don't want this canvas to receive input events; disable its GraphicRaycaster
            var gr = canvasGO.AddComponent<GraphicRaycaster>();
            gr.enabled = false;
            // Make sure canvas persists in edit/play (optional) — not modifying hideFlags so it's visible in hierarchy.
        }

        // Reparent the glowImage under the topmost canvas without changing layout (false)
        glowImage.transform.SetParent(createdOverlayCanvas.transform, false);

        // Stretch to full screen
        RectTransform rt = glowImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Ensure it won't block UI raycasts
        glowImage.raycastTarget = false;

        // Put as last sibling so it's drawn last within that canvas
        glowImage.transform.SetAsLastSibling();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        HandleContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        HandleContact(collision.collider);
    }

    private void HandleContact(Collider other)
    {
        // Quick check by tag or by presence of PlayerController
        if (!other.CompareTag(playerTag) && other.GetComponent<PlayerController>() == null) return;

        // If already teleporting, ignore additional contacts
        if (isTeleporting) return;

        Transform playerTransform = other.transform;
        PlayerController playerController = other.GetComponent<PlayerController>();
        CharacterController charController = other.GetComponent<CharacterController>();

        Vector3 destination = targetTransform != null ? targetTransform.position : targetPosition;

        // If we have a PlayerController run the teleport via coroutine (with glow if assigned).
        if (playerController != null)
        {
            StartCoroutine(TeleportWithGlowRoutine(playerTransform, destination, playerController, charController));
        }
        else
        {
            // fallback immediate teleport (preserve previous safe-charactercontroller handling)
            if (charController != null) charController.enabled = false;
            playerTransform.position = destination;
            if (charController != null) charController.enabled = true;
        }
    }

    private IEnumerator TeleportWithGlowRoutine(Transform playerTransform, Vector3 destination, PlayerController pc, CharacterController cc)
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        // Lock inputs immediately
        pc.inputsLocked = true;

        // If no glow image assigned, do a short wait and teleport
        if (glowImage == null)
        {
            if (cc != null) cc.enabled = false;
            yield return new WaitForSeconds(lockDuration);
            playerTransform.position = destination;
            if (cc != null) cc.enabled = true;
            pc.inputsLocked = false;
            isTeleporting = false;
            yield break;
        }

        // Ensure the overlay (and its Canvas) are present and configured to be topmost
        EnsureTopmostCanvasForGlow();

        // Activate overlay for the effect
        glowImage.gameObject.SetActive(true);
        Color baseColor = glowImage.color;
        baseColor.a = 0f;
        glowImage.color = baseColor;

        float half = Mathf.Max(0.001f, glowDuration * 0.5f);
        float t = 0f;

        // Grow phase (0 -> peak)
        while (t < half)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / half);
            float curveVal = glowCurve != null ? glowCurve.Evaluate(norm) : norm;
            Color c = glowImage.color;
            c.a = Mathf.Lerp(0f, glowPeakAlpha, curveVal);
            glowImage.color = c;
            yield return null;
        }

        // Teleport while overlay at peak
        if (cc != null) cc.enabled = false; // disable CC before moving to avoid physics snapbacks
        playerTransform.position = destination;
        yield return null; // ensure transform updated on next frame

        // Fade phase (peak -> 0)
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float norm = Mathf.Clamp01(t / half);
            float curveVal = glowCurve != null ? glowCurve.Evaluate(1f - norm) : (1f - norm);
            Color c = glowImage.color;
            c.a = Mathf.Lerp(0f, glowPeakAlpha, curveVal);
            glowImage.color = c;
            yield return null;
        }

        // Ensure fully transparent and hide overlay so it only appears during teleport
        Color endColor = glowImage.color;
        endColor.a = 0f;
        glowImage.color = endColor;
        glowImage.gameObject.SetActive(false);

        if (cc != null) cc.enabled = true;

        // small extra wait to match original lockDuration behavior (optional)
        if (lockDuration > 0f)
            yield return new WaitForSeconds(lockDuration);

        pc.inputsLocked = false;
        isTeleporting = false;
    }

    private IEnumerator UnlockInputsAfterDelay(PlayerController pc)
    {
        yield return new WaitForSeconds(lockDuration);
        if (pc != null) pc.inputsLocked = false;
    }
}