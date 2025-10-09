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
    [SerializeField] Weapon weapon;

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
        
        //return if the time since last final attack is to fast or no 
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
        playerController.inputsLocked = true;

        anim.runtimeAnimatorController = combo[comboCounter].animatorOV;
        anim.Play("Attack", 0, 0);
        weapon.damage = combo[comboCounter].damage;

        comboCounter++;
        lastClickedTime = Time.time;

        if(comboCounter >= combo.Count)
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
        Debug.Log("reset combo");
        comboCounter = 0;
        lastComboEnd = Time.time;
    }
}
