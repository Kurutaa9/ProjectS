using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ObjectSoundEmitter : MonoBehaviour
{
    [Header("Sound Settings")]
    public AudioClip soundClip;
    [Range(0f, 1f)] public float volume = 1.0f;
    public bool loop = true;
    public bool playOnAwake = true;

    [Header("3D Settings")]
    public float minDistance = 1.0f;
    public float maxDistance = 20.0f;
    
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Configure AudioSource for 3D Sound
        audioSource.spatialBlend = 1.0f; // 1.0 means fully 3D
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.loop = loop;
        audioSource.playOnAwake = playOnAwake;
        audioSource.clip = soundClip;
        audioSource.volume = volume;

        if (playOnAwake && soundClip != null)
        {
            audioSource.Play();
        }
    }

    private void OnValidate()
    {
        // Update settings in editor if changed while playing
        if (audioSource != null)
        {
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.volume = volume;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the sound range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}
