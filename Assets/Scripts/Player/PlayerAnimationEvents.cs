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
        weaponCanDamageTrue(0);
    }

    public void weaponCanDamageTrue(int index)
    {
        if (playerCombat.weapons != null && index >= 0 && index < playerCombat.weapons.Count && playerCombat.weapons[index] != null)
        {
            playerCombat.weapons[index].StartAttack();
            playerCombat.weapons[index].canDamage = true;
        }
    }

    public void weaponCanDamageFalse()
    {
        weaponCanDamageFalse(0);
    }

    public void weaponCanDamageFalse(int index)
    {
        if (playerCombat.weapons != null && index >= 0 && index < playerCombat.weapons.Count && playerCombat.weapons[index] != null)
        {
            playerCombat.weapons[index].canDamage = false;
            playerCombat.weapons[index].EndAttack();
        }
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
