using Cinemachine.Utility;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.InputSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public float playerSpeed;
    public float jumpHeight;

    [Header("Input")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference lockOnTargetAction;

    [Header("orientation")]
    public Transform orientation;

    [Header("Object")]
    public GameObject playerObj;

    [Header("Animation")]
    public Animator anim;

    [Header("Layer masks")]
    public LayerMask ground;

    [Header("Player Controller")]
    public CharacterController controller;

    private float gravity = -9.81f;
    private Vector3 playerVelocity;
    private bool grounded;

    private Vector3 move;
    private Vector3 moveDir;

    //combat controls
    private bool lockedOnTarget = true;

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        lockOnTargetAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        lockOnTargetAction.action.Disable();
    }

    void Update()
    {
        grounded = Physics.Raycast(transform.position + controller.center, Vector3.down, controller.height / 2f + 0.1f, ground);
        //if player hits ground and is falling, stop falling...
        if (grounded && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        // Read input   
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        move = new Vector3(input.x, 0, input.y);
        move = Vector3.ClampMagnitude(move, 1f);
        moveDir = orientation.forward * move.z + orientation.right * move.x;
        moveDir.y = 0f;
        moveDir = moveDir.normalized;

        // Jump
        if (jumpAction.action.triggered && grounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;

        //set the player facing direction
        if(moveDir.magnitude > 0.1f && !lockedOnTarget)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            playerObj.transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetRotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        }

        // Combine horizontal and vertical movement
        Vector3 finalMove = (moveDir * playerSpeed) + (playerVelocity.y * Vector3.up);

        //playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(finalMove * Time.deltaTime);

        combatControls();
        updateAnimations();
    }

    private void combatControls()
    {
        if (lockOnTargetAction.action.triggered)
        {
            lockedOnTarget = !lockedOnTarget;
            Debug.Log(lockedOnTarget);
        }
    }

    private void updateAnimations()
    {
        anim.SetBool("lockedOnTarget", lockedOnTarget);
        if (lockedOnTarget)
        {
            anim.SetFloat("strafevelx", controller.velocity.x);
            anim.SetFloat("strafevely", controller.velocity.z);
        }
        else
        {
            if(moveDir.magnitude >= 0.1f)
            {
                anim.SetBool("walkForward", true);
            } 
            else
            {
                anim.SetBool("walkForward", false);
            }
        }
    }
}
