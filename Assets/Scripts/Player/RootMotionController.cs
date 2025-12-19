using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RootMotionController : MonoBehaviour
{
    public GameObject playerParent;
    public Animator anim;
    public bool useRootMotion = true;
    private CharacterController characterController;
    private NavMeshAgent navMeshAgent;

    private void Start()
    {
        if (playerParent != null)
        {
            characterController = playerParent.GetComponent<CharacterController>();
            navMeshAgent = playerParent.GetComponent<NavMeshAgent>();
            if (navMeshAgent != null)
            {
                navMeshAgent.updatePosition = !useRootMotion;
                navMeshAgent.updateRotation = false;
            }
        }
    }

    private void OnAnimatorMove()
    {
        anim.applyRootMotion = useRootMotion;

        if (characterController != null)
        {
            characterController.Move(anim.deltaPosition);
            playerParent.transform.rotation *= anim.deltaRotation;
        }
        else if (navMeshAgent != null)
        {
            if (useRootMotion)
            {
                navMeshAgent.updatePosition = false;
                navMeshAgent.nextPosition = playerParent.transform.position;
                navMeshAgent.Move(anim.deltaPosition);
                playerParent.transform.position = navMeshAgent.nextPosition;
                playerParent.transform.rotation *= anim.deltaRotation;
            }
            else
            {
                navMeshAgent.updatePosition = true;

                playerParent.transform.rotation *= anim.deltaRotation;
            }
        }
    }
}
