using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerCombat : MonoBehaviour
{
    public PlayerController playerController;

    public List<AttackSO> combo;
    public AttackSO heavyAttack; // New Heavy Attack
    float lastClickedTime;
    float lastComboEnd;
    int comboCounter;
    public bool attackbuffer = false;
    public bool heavyAttackBuffer = false; // New Buffer for Heavy Attack

    // index of attack currently executing (set when attack starts)
    int currentExecutingAttackIndex = -1;
    // index of the last completed attack (set when animation finished)
    int lastCompletedAttackIndex = -1;

    // cooldowns after finishing an attack. First two attacks have a short cooldown so
    // the player can cancel and re-start quickly. Finishing the full combo (last attack)
    // applies a longer cooldown.
    [Tooltip("Short cooldown after finishing attack (for early cancel) in seconds")]
    public float shortFinishCooldown = 0.01f;
    [Tooltip("Full cooldown applied after finishing the final combo attack in seconds")]
    public float fullFinishCooldown = 0.05f;
    // currently active cooldown to compare against lastComboEnd
    float lastComboCooldown = 0.05f;

    int debugCounter = 0;

    public Animator anim;
    [SerializeField] public List<Weapon> weapons = new List<Weapon>();
    public AudioSource audioSource;
    
    // Track if VFX has been played for the current attack to avoid duplicates
    private bool hasPlayedVFX = false;

    void Start()
    {
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }


    void Update()
    {
        CheckVFXTrigger();
        ExitAttack();
        HandleAttacks();
    }

    void CheckVFXTrigger()
    {
        if (playerController.IsAttacking && !hasPlayedVFX && weapons != null && weapons.Count > 0)
        {
            // Check if we are in an attack state
            if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                if (Time.time - lastClickedTime < 0.1f) return;

                // Determine which config to use based on attack type
                // Heavy Attack (-1) uses heavy config
                // Combo Attack (>= 2) uses light/combo config
                bool isHeavy = (currentExecutingAttackIndex == -1);
                bool isComboFinisher = (currentExecutingAttackIndex >= 2);

                if (!isHeavy && !isComboFinisher) return;

                foreach (var w in weapons)
                {
                    if (w == null) continue;
                    // Get the appropriate trigger point
                    float triggerPoint = isHeavy ? w.heavyAttackVFX.triggerPoint : w.lightAttackVFX.triggerPoint;

                    // Check if we passed the trigger point
                    if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= triggerPoint)
                    {
                        w.PlayAttackVFX(isHeavy);
                    }
                }
                // Assume if we checked one, we checked all. 
                // Ideally we should track per weapon, but for now let's just set flag true.
                if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= (isHeavy ? weapons[0].heavyAttackVFX.triggerPoint : weapons[0].lightAttackVFX.triggerPoint))
                {
                    hasPlayedVFX = true;
                }
            }
        }
    }

    public void HandleAttacks()
    {
        // Priority: Heavy Attack > Light Attack (or whatever preference)
        // Check Heavy Attack first
        if (heavyAttackBuffer)
        {
            PerformHeavyAttack();
            return;
        }

        // Check Light Attack
        if (attackbuffer)
        {
            Attack();
        }
    }

    public void PerformHeavyAttack()
    {
        // Basic checks: cooldowns, animation state
        if (Time.time - lastClickedTime <= 0.2f || Time.time - lastComboEnd <= lastComboCooldown)
        {
            return;
        }

        // If already attacking (light or heavy), wait until near end
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.9f)
        {
            return;
        }

        CancelInvoke("EndCombo");

        // Check Stamina
        float staminaCost = (heavyAttack != null) ? heavyAttack.staminaCost : 0f;
        if (playerController != null && playerController.playerStats != null)
        {
            if (!playerController.playerStats.CanPerformAction(staminaCost))
            {
                heavyAttackBuffer = false;
                return;
            }
            else
            {
                playerController.playerStats.ConsumeStamina(staminaCost);
            }
        }

        heavyAttackBuffer = false;
        playerController.IsAttacking = true;
        playerController.inputsLocked = true;

        // Reset combo counter because heavy attack breaks the light combo chain
        comboCounter = 0;
        currentExecutingAttackIndex = -1; // Not part of the combo list

        if (heavyAttack != null)
        {
            anim.runtimeAnimatorController = heavyAttack.animatorOV;
            if (weapons != null)
            {
                foreach (var w in weapons)
                {
                    if (w) w.damage = heavyAttack.damage;
                }
            }
            
            // Play Sound
            PlayAttackSounds(heavyAttack);
        }
        
        anim.Play("Attack", 0, 0);
        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w) w.StartAttack();
            }
        }
        hasPlayedVFX = false; // Reset VFX flag
        
        // Enable Heavy Trail
        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w) w.EnableTrail(true);
            }
        }

        lastClickedTime = Time.time;
        
        // We treat heavy attack as a "full commit", so we might want a longer cooldown after it
        // For now, we rely on ExitAttack -> EndCombo logic. 
        // Since currentExecutingAttackIndex is -1, EndCombo will treat it as "not full combo" (short cooldown)
        // unless we modify EndCombo. Let's modify EndCombo to handle this if needed.
        // Actually, let's set a flag or just let it be short cooldown for now.
    }

    public void Attack()
    {
        // return if the time since last click is too fast, combo is maxed, no buffer,
        // or we are still inside the cooldown that follows the last completed attack
        if (Time.time - lastClickedTime <= 0.2f || comboCounter >= combo.Count || !attackbuffer || Time.time - lastComboEnd <= lastComboCooldown)
        {
            return;
        }

        //return if animation is still not 90% complete
        if (anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.9f)
        {
            return; 
        }

        CancelInvoke("EndCombo");
        // Before starting, check stamina cost for this attack and consume it.
        float attackStaminaCost = 0f;
        if (combo != null && combo.Count > comboCounter)
        {
            attackStaminaCost = combo[comboCounter].staminaCost;
        }

        // If no stamina available for this attack, discard the buffered input and abort.
        if (playerController != null && playerController.playerStats != null)
        {
            if (!playerController.playerStats.CanPerformAction(attackStaminaCost))
            {
                // Player lacks stamina: end the combo so we don't resume mid-combo
                // once stamina regenerates. Apply the short finish cooldown and
                // reset combo trackers.
                attackbuffer = false;
                comboCounter = 0;
                lastComboEnd = Time.time;
                lastComboCooldown = shortFinishCooldown;
                currentExecutingAttackIndex = -1;
                lastCompletedAttackIndex = -1;
                return;
            }
            else
            {
                playerController.playerStats.ConsumeStamina(attackStaminaCost);
            }
        }

        attackbuffer = false;
        playerController.IsAttacking = true;
        playerController.inputsLocked = true; //used too lock player inputs so cant do anything while attacking
        // mark which attack index we're executing (used when the animation completes)
        currentExecutingAttackIndex = comboCounter;

        anim.runtimeAnimatorController = combo[comboCounter].animatorOV; //overirdes current animator with the new animations for that specific combo
        
        // Play Sound
        PlayAttackSounds(combo[comboCounter]);

        anim.Play("Attack", 0, 0); // play the attack animation that has been overwritten
        
        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w)
                {
                    w.damage = combo[comboCounter].damage; //set the weapon damage according to the current combo
                    w.StartAttack();
                }
            }
        }
        hasPlayedVFX = false; // Reset VFX flag

        // Handle Trails and Effects
        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w)
                {
                    // If it's the 3rd attack (index 2) or later
                    if (comboCounter >= 2)
                    {
                        w.EnableTrail(true);
                    }
                    else
                    {
                        w.EnableTrail(false);
                    }
                }
            }
        }

        comboCounter++;
        lastClickedTime = Time.time;

        // Do NOT set lastComboEnd here; wait until the animation actually finishes
        // (EndCombo) so we can apply different cooldowns depending on which attack finished.

    }

    void ExitAttack()
    {
        if(playerController.IsAttacking && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            // record which attack just finished so EndCombo can choose the proper cooldown
            lastCompletedAttackIndex = currentExecutingAttackIndex;
            Invoke("EndCombo", 0.2f); //end combo after 0.2 of animation finishing
            playerController.IsAttacking = false;
            if (!playerController.isTakingHit)
            {
                playerController.inputsLocked = false;
            }
        }
    }

    void EndCombo()
    {
        comboCounter = 0;
        lastComboEnd = Time.time;

        // if the last completed attack index equals the last combo entry, the player
        // committed to the full combo: apply the full cooldown. Otherwise allow a
        // short cooldown so the player can quickly re-start from the beginning.
        if (lastCompletedAttackIndex == combo.Count - 1)
        {
            lastComboCooldown = fullFinishCooldown;
            // If the player committed to the full combo, clear any buffered attack
            // input so a press that occurred during the final attack doesn't
            // immediately start a new combo after the cooldown.
            attackbuffer = false;
        }
        else
        {
            lastComboCooldown = shortFinishCooldown;
        }

        // reset temporary trackers
        currentExecutingAttackIndex = -1;
        lastCompletedAttackIndex = -1;
    }

    public void InterruptCombo()
    {
        // Stop any delayed attack sounds
        StopAllCoroutines();

        // clear combo state and apply a short finish cooldown so player can quickly resume
        comboCounter = 0;
        lastCompletedAttackIndex = -1;
        currentExecutingAttackIndex = -1;
        lastComboEnd = Time.time;
        lastComboCooldown = shortFinishCooldown;
        attackbuffer = false;
        heavyAttackBuffer = false;

        // Ensure weapon damage is disabled
        if (weapons != null)
        {
            foreach (var w in weapons)
            {
                if (w)
                {
                    w.canDamage = false;
                    w.EndAttack();
                }
            }
        }

        // release player input/attack locks (controller may override during GetHit)
        if (playerController != null)
        {
            playerController.IsAttacking = false;
            playerController.inputsLocked = false;
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

    private void PlayAttackSounds(AttackSO attack)
    {
        if (attack == null || audioSource == null) return;

        // Play new list of sounds
        if (attack.soundEffects != null)
        {
            foreach (var sfx in attack.soundEffects)
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
    }
}
