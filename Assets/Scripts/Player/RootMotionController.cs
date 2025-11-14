using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RootMotionController : MonoBehaviour
{
    public GameObject playerParent;
    public Animator anim;

    private void OnAnimatorMove()
    {
        anim.applyRootMotion = true;

        playerParent.transform.position += anim.deltaPosition;
        playerParent.transform.rotation *= anim.deltaRotation;
    }
}
