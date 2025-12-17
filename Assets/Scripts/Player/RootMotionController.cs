using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionController : MonoBehaviour
{
    public GameObject playerParent;
    public Animator anim;
    private CharacterController characterController;

    private void Start()
    {
        if (playerParent != null)
        {
            characterController = playerParent.GetComponent<CharacterController>();
        }
    }

    private void OnAnimatorMove()
    {
        anim.applyRootMotion = true;

        if (characterController != null)
        {
            characterController.Move(anim.deltaPosition);
            playerParent.transform.rotation *= anim.deltaRotation;
        }
    }
}
