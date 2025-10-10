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
        //return if the time since last final attack is too fast or no 
        if (Time.time - lastClickedTime <= 0.2f || comboCounter >= combo.Count || !attackbuffer)
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

        anim.runtimeAnimatorController = combo[comboCounter].animatorOV; //overirdes current animator with the new animations for that specific combo
        anim.Play("Attack", 0, 0); // play the attack animation that has been overwritten
        weapon.damage = combo[comboCounter].damage; //set the weapon damage according to the current combo
        weapon.StartAttack();

        comboCounter++;
        lastClickedTime = Time.time;

        if(comboCounter >= combo.Count) //reset combo if reached max combo
        {
            comboCounter = 0;
        }

    }

    void ExitAttack()
    {
        if(anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f && anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Invoke("EndCombo", 0.7f); //end combo after 0.7 of animation finishing
            playerController.IsAttacking = false;
            playerController.inputsLocked = false;
        }
    }

    void EndCombo()
    {
        comboCounter = 0;
        lastComboEnd = Time.time;
    }
}
