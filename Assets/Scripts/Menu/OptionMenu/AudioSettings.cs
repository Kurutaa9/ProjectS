using UnityEngine;

public class AudioSettings : MonoBehaviour
{
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }
}
