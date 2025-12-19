using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Attacks/Normal Attack")]
public class AttackSO : ScriptableObject
{
    public AnimatorOverrideController animatorOV;
    public float damage;
    public float staminaCost;

    [System.Serializable]
    public class SoundEffect
    {
        public AudioClip clip;
        [Tooltip("Delay in seconds before playing the sound")]
        public float delay = 0f;
    }

    [Header("Audio")]
    public List<SoundEffect> soundEffects = new List<SoundEffect>();
    //test
}
