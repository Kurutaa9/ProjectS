using System.Collections;
using UnityEngine;

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

    [Tooltip("How long the player inputs stay locked after teleporting.")]
    public float lockDuration = 0.1f;

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

        Transform playerTransform = other.transform;
        PlayerController playerController = other.GetComponent<PlayerController>();
        CharacterController charController = other.GetComponent<CharacterController>();

        Vector3 destination = targetTransform != null ? targetTransform.position : targetPosition;

        // Disable CharacterController before changing transform to avoid unwanted collision/physics snaps
        if (charController != null) charController.enabled = false;

        // Lock inputs (if PlayerController available) so the player doesn't fight the teleport
        if (playerController != null) playerController.inputsLocked = true;

        // Teleport
        playerTransform.position = destination;

        // Re-enable CharacterController
        if (charController != null) charController.enabled = true;

        // Unlock inputs after a short delay
        if (playerController != null) StartCoroutine(UnlockInputsAfterDelay(playerController));
    }

    private IEnumerator UnlockInputsAfterDelay(PlayerController pc)
    {
        yield return new WaitForSeconds(lockDuration);
        if (pc != null) pc.inputsLocked = false;
    }
}