using UnityEngine;

// Marker component for lock-on targets. Place this on a child object of an enemy
// (e.g., a head or chest anchor) and ensure it has a Collider (can be a small trigger).
// The PlayerController will prefer colliders on the lockTarget Layer, but will fall back
// to any Collider that has this component if none are found on the layer.
[RequireComponent(typeof(Collider))]
public class LockOnAnchor : MonoBehaviour
{
    [Tooltip("Optional: if true, force this collider to be a trigger.")]
    public bool forceTrigger = true;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (forceTrigger && col) col.isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (forceTrigger && col) col.isTrigger = true;
    }
}
