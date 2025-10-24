using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerCombat : MonoBehaviour
{
    public PlayerController playerController;

    public List<AttackSO> combo;
    float lastClickedTime;
    float lastComboEnd;
    int comboCounter;
    public bool attackbuffer = false;

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
    [SerializeField] public Weapon weapon;

    void Start()
    {
        
    }


    void Update()
    {
        ExitAttack();
        Attack();
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
        attackbuffer = false;
        playerController.IsAttacking = true;
        playerController.inputsLocked = true; //used too lock player inputs so cant do anything while attacking
        // mark which attack index we're executing (used when the animation completes)
        currentExecutingAttackIndex = comboCounter;

        anim.runtimeAnimatorController = combo[comboCounter].animatorOV; //overirdes current animator with the new animations for that specific combo
        anim.Play("Attack", 0, 0); // play the attack animation that has been overwritten
        weapon.damage = combo[comboCounter].damage; //set the weapon damage according to the current combo
        weapon.StartAttack();

        comboCounter++;
        lastClickedTime = Time.time;

        // Do NOT set lastComboEnd here; wait until the animation actually finishes
        // (EndCombo) so we can apply different cooldowns depending on which attack finished.

    }

    void ExitAttack()
    {
        if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            // record which attack just finished so EndCombo can choose the proper cooldown
            lastCompletedAttackIndex = currentExecutingAttackIndex;
            Invoke("EndCombo", 0.2f); //end combo after 0.2 of animation finishing
            playerController.IsAttacking = false;
            playerController.inputsLocked = false;
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
}
