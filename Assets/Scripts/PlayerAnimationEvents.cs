using System.Collections;
using System.Collections.Generic;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public PlayerController playerController;

    public void setRollingFalse()
    {
        playerController.isRolling = false;
        playerController.anim.SetBool("IsRolling", false);
        playerController.inputsLocked = false;
    }

    public void setAttackingFalse()
    {
        playerController.isAttacking = false;
        playerController.anim.SetBool("IsAttacking", false);
    }
}
