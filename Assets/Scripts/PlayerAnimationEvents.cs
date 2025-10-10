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
}
