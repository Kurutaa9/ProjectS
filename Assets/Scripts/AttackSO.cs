using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Attacks/Normal Attack")]
public class AttackSO : ScriptableObject
{
    public AnimatorOverrideController animatorOV;
    public float damage;
    public float staminaCost;
    [Tooltip("Multiplier for the chance to stun the enemy (1.0 = normal, >1.0 = higher chance)")]
    public float stunChanceMultiplier = 1.0f;

    [System.Serializable]
    public class SoundEffect
    {
        public AudioClip clip;
        [Tooltip("Delay in seconds before playing the sound")]
        public float delay = 0f;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Audio")]
    public List<SoundEffect> soundEffects = new List<SoundEffect>();
    //test
}
