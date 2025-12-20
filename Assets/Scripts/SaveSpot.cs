using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class SaveSpot : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Input System action used to interact (performed toggles the popup)")] public InputActionReference interactAction;
    [Tooltip("Player tag to detect in trigger")] public string playerTag = "Player";

    [Header("UI Popup")] 
    [Tooltip("UI Canvas to show when interacting (enable/disable GameObject)")]
    public Canvas popupCanvas;
    [Tooltip("Pause the game (Time.timeScale = 0) while the popup is open")] public bool pauseOnOpen = true;
    [Tooltip("Unlock and show cursor while popup is open")] public bool manageCursor = true;

    [Header("Checkpoint")]
    [Tooltip("If true, mark this spot as the latest checkpoint when opened")] public bool setCheckpointOnOpen = true;
    [Tooltip("Optional override transform to use as the checkpoint (e.g., a spawn point child)")] public Transform checkpointOverride;

    private bool playerInRange = false;
    private bool isOpen = false;

    [Header("Player Input Lock (optional)")]
    [Tooltip("Lock player inputs while popup is open")] public bool lockPlayerInputsOnOpen = true;
    [Tooltip("Reference to PlayerController to lock/unlock inputs; auto-assigned from trigger if null")] public PlayerController playerController;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Awake()
    {
        if (popupCanvas)
        {
            popupCanvas.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            if (playerController == null)
            {
                playerController = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
        }
    }

    private void OnEnable()
    {
        if (interactAction != null) interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null) interactAction.action.Disable();
    }

    private void Update()
    {
        if (!playerInRange) return;
        if (interactAction != null)
        {
            // triggered is true only on the frame performed
            if (interactAction.action.triggered)
            {
                playerController.RestAtSaveSpot();
                if (isOpen) ClosePopup(); else OpenPopup();
            }
        }
    }

    public void OpenPopup()
    {
        if (isOpen) return;
        isOpen = true;
        if (popupCanvas)
        {
            popupCanvas.gameObject.SetActive(true);
        }
        if (pauseOnOpen)
        {
            Time.timeScale = 0f;
        }
        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (setCheckpointOnOpen)
        {
            Transform t = checkpointOverride != null ? checkpointOverride : transform;
            CheckpointManager.SetCheckpoint(t);
        }
        if (lockPlayerInputsOnOpen && playerController != null)
        {
            playerController.inputsLocked = true;
            playerController.attackLocked = true;
            playerController.isSprinting = false; // safety
            playerController.PlayRestEnterSound();
        }
    }

    public void ClosePopup()
    {
        if (!isOpen) return;
        isOpen = false;
        if (popupCanvas)
        {
            popupCanvas.gameObject.SetActive(false);
        }
        if (pauseOnOpen)
        {
            Time.timeScale = 1f;
        }
        if (manageCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (lockPlayerInputsOnOpen && playerController != null)
        {
            playerController.inputsLocked = false;
            playerController.attackLocked = false;
            playerController.PlayRestExitSound();
        }
    }
}
