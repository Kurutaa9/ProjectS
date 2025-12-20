using UnityEngine;

public static class CheckpointManager
{
    private static bool _hasCheckpoint = false;
    private static Vector3 _checkpointPosition;
    private static Quaternion _checkpointRotation;

    public static bool HasCheckpoint => _hasCheckpoint;
    public static Vector3 CheckpointPosition => _checkpointPosition;
    public static Quaternion CheckpointRotation => _checkpointRotation;

    public static void SetCheckpoint(Transform t)
    {
        if (t == null) return;
        _checkpointPosition = t.position;
        _checkpointRotation = t.rotation;
        _hasCheckpoint = true;
    }

    public static void ResetCheckpoint()
    {
        _hasCheckpoint = false;
        _checkpointPosition = Vector3.zero;
        _checkpointRotation = Quaternion.identity;
    }
}
