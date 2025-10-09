using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
    public PlayerController playerController;

    public void setRollingFalse()
    {
        playerController.isRolling = false;
        playerController.anim.SetBool("IsRolling", false);
        playerController.inputsLocked = false;
        playerController.attackLocked = false;
        Debug.Log("Set input locked fasle form rolling");
    }

    public void setInputLock()
    {
        playerController.inputsLocked = true;
    }

    public void resetInputLock()
    {
        playerController.inputsLocked = false;
    }
}
