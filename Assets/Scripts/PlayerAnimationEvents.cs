using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public PlayerController playerController;
    public PlayerCombat playerCombat;

    public void setRollingFalse()
    {
        playerController.isRolling = false;
        playerController.anim.SetBool("IsRolling", false);
        playerController.inputsLocked = false;
        playerController.attackLocked = false;
    }

    public void setInputLock()
    {
        playerController.inputsLocked = true;
    }

    public void resetInputLock()
    {
        playerController.inputsLocked = false;
    }

    public void weaponCanDamageTrue()
    {
        playerCombat.weapon.canDamage = true;
    }

    public void weaponCanDamageFalse()
    {
        playerCombat.weapon.canDamage = false;
    }

    // Dodge i-frames via animation events
    // Add these two events to the dodge animation timeline at the start and end of the invincibility window
    public void dodgeInvincibilityOn()
    {
        if (playerController != null && playerController.playerStats != null)
        {
            playerController.playerStats.SetInvincible(true);
            Debug.Log($"[PlayerIFrames] ON at t={Time.time:F2}");
        }
    }

    public void dodgeInvincibilityOff()
    {
        if (playerController != null && playerController.playerStats != null)
        {
            playerController.playerStats.SetInvincible(false);
            Debug.Log($"[PlayerIFrames] OFF at t={Time.time:F2}");
        }
    }
}
