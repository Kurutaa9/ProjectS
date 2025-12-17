using UnityEngine;
using UnityEngine.AI;

// Keeps a Rigidbody kinematic (so it can't be pushed) while still applying gravity.
// If a NavMeshAgent is present and enabled, we let the agent control positioning
// and we only ensure the body remains kinematic/unaffected by external forces.
[RequireComponent(typeof(Rigidbody))]
public class BossKinematicGravity : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Force Rigidbody to be kinematic and disable built-in gravity")] public bool enforceKinematic = true;
    [Tooltip("Scale applied to Physics.gravity.y")] public float gravityScale = 1.0f;
    [Tooltip("Layers considered ground for grounding checks")] public LayerMask groundLayers = ~0;

    [Header("Ground Check")]
    [Tooltip("Optional transform used as the ground check origin (e.g., feet). If null, uses this transform.")] public Transform groundCheck;
    [Tooltip("Radius used when sphere checking for ground contact")] public float groundCheckRadius = 0.25f;
    [Tooltip("Extra distance for ground probe to avoid floating")] public float groundCheckExtra = 0.05f;

    private Rigidbody rb;
    private NavMeshAgent agent;
    private float verticalVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();
        if (groundCheck == null) groundCheck = transform;

        if (enforceKinematic)
        {
            rb.isKinematic = true;
            rb.useGravity = false; // we'll apply manual gravity
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void FixedUpdate()
    {
        // If we have a NavMeshAgent currently enabled, assume it manages position.
        if (agent != null && agent.enabled)
        {
            // Ensure we stay kinematic and unaffected by physics pushes.
            if (enforceKinematic && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            return;
        }

        // Manual gravity step for kinematic bodies.
        if (rb.isKinematic)
        {
            // Ground probe
            Vector3 origin = groundCheck.position + Vector3.up * 0.1f;
            float probeDistance = groundCheckRadius + groundCheckExtra;
            bool grounded = Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
                                               out RaycastHit hit, probeDistance, groundLayers,
                                               QueryTriggerInteraction.Ignore);

            if (grounded && verticalVelocity <= 0f)
            {
                // Snap to ground and reset vertical velocity
                Vector3 pos = rb.position;
                pos.y = hit.point.y;
                rb.MovePosition(pos);
                verticalVelocity = 0f;
            }
            else
            {
                verticalVelocity += Physics.gravity.y * gravityScale * Time.fixedDeltaTime;
                Vector3 delta = new Vector3(0f, verticalVelocity * Time.fixedDeltaTime, 0f);
                rb.MovePosition(rb.position + delta);
            }
            Debug.Log(grounded);
        }
    }
}
