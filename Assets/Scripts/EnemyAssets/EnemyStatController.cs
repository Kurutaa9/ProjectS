using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyStatController : MonoBehaviour
{
    [SerializeField] private CharacterStatsSO baseStats;

    [System.Serializable]
    public class SoundEffect
    {
        public AudioClip clip;
        public float delay;
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public List<SoundEffect> hitSounds = new List<SoundEffect>();
    public List<SoundEffect> deathSounds = new List<SoundEffect>();

    private float currentHealth;
    private bool hasTakenDamage;
    public UnityEvent<bool> OnDamageStateChanged;
    public UnityEvent<float> OnHealthChanged;

    public UnityEvent OnTakeDamage;
    public UnityEvent OnDeath;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        currentHealth = baseStats.maxHealth;
        hasTakenDamage = false;
        OnHealthChanged.Invoke(currentHealth);
        OnDamageStateChanged.Invoke(hasTakenDamage);
    }

    void Update()
    {

    }

    public void TakeDamage(float amount)
    {
        //Debug.Log($"enemy take {amount} damage");
        if (!hasTakenDamage)
        {
            hasTakenDamage = true;
            OnDamageStateChanged.Invoke(hasTakenDamage);
            //Debug.Log($"invoked {hasTakenDamage}");
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        OnHealthChanged.Invoke(currentHealth);

        // Stop previous sounds
        if (audioSource != null) audioSource.Stop();
        StopAllCoroutines();

        if (currentHealth > 0f)
        {
            PlaySounds(hitSounds);
            OnTakeDamage.Invoke();
        }
        else
        {
            PlaySounds(deathSounds);
            OnDeath.Invoke();
        }
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlaySounds(List<SoundEffect> sounds)
    {
        if (audioSource == null || sounds == null) return;
        foreach (var sfx in sounds)
        {
            if (sfx.clip != null)
            {
                if (sfx.delay > 0)
                    StartCoroutine(PlaySoundDelayed(sfx.clip, sfx.delay));
                else
                    audioSource.PlayOneShot(sfx.clip);
            }
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    public float GetMaxHealth()
    {
        return baseStats.maxHealth;
    }

    public float GetBaseDamage()
    {
        return baseStats.baseDamage;
    }

    public int GetSoulsReward()
    {
        return baseStats.soulsReward;
    }

    public bool HasTakenDamage()
    {
        return hasTakenDamage;
    }

    public void ResetStats()
    {
        currentHealth = baseStats.maxHealth;
        hasTakenDamage = false;
        OnHealthChanged.Invoke(currentHealth);
        OnDamageStateChanged.Invoke(hasTakenDamage);
    }
}
