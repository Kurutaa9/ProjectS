using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public float playerSpeed;
    public float jumpHeight;
    public float rotationSpeed;

    [Header("Input")]
    public PlayerRotateController rotateController;
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference AttackAction;
    public InputActionReference rollAction;
    public InputActionReference lockOnTargetAction;

    [Header("orientation")]
    public Camera cam;
    public Transform orientation;

    [Header("Object")]
    public GameObject playerObj;

    [Header("Animation")]
    public Animator anim;

    [Header("Layer masks")]
    public LayerMask ground;
    public LayerMask lockTarget;

    [Header("Player Controller")]
    public CharacterController controller;

    [Header("TargetLock Settings")]
    public float targetLockRange;
    public float targetSwitchThreshold;
    public float targetSwitchCooldown;
    private float lastSwitchTime = 0f;

    private float gravity = -9.81f;
    private Vector3 playerVelocity;
    private bool grounded;

    private Vector3 move;
    private Vector3 moveDir;

    //combat controls
    public bool lockedOnTarget = false;
    public GameObject currentTarget;

    //rolling
    [Header("Roll Settings")]
    public bool isRolling = false;
    public float rollDistance = 4f;
    public float rollSpeedMultiplier = 2f;
    private Quaternion rollTargetRotation;
    private Vector3 rollDirection;
    public bool inputsLocked = false;

    //Attacking
    public bool isAttacking = false;

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        lockOnTargetAction.action.Enable();
        AttackAction.action.Enable();
        rollAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        lockOnTargetAction.action.Disable();
        AttackAction.action.Disable();
        rollAction.action.Disable();
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
        if (!inputsLocked)
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            move = new Vector3(input.x, 0, input.y);
        } else
        {
            move = Vector3.zero;
        }
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

        //set the player facing direction (only when in freelook) otherwise lock the camera to target
        if (isRolling)
        {
            playerObj.transform.rotation = Quaternion.Slerp(
                playerObj.transform.rotation,
                rollTargetRotation,
                rotationSpeed * 2f * Time.deltaTime);
        }
        else if (lockedOnTarget)
        {
            Vector3 targetDirection = currentTarget.transform.position - playerObj.transform.position;
            targetDirection.y = 0;

            if (targetDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            CheckForTargetSwitch();
        }
        else if (moveDir.magnitude > 0.1f && !lockedOnTarget)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            Quaternion finalRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, targetRotation.eulerAngles.y, transform.rotation.eulerAngles.z);
            //interpolates the playerobj rotation so when using wasd, the player rotation is smooth
            playerObj.transform.rotation = Quaternion.Slerp(playerObj.transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
        }

        // Combine horizontal and vertical movement
        Vector3 finalMove;

        if (isRolling)
        {
            finalMove = (rollDirection * playerSpeed * rollSpeedMultiplier) + (playerVelocity.y * Vector3.up);
        }
        else
        {
            finalMove = (moveDir * playerSpeed) + (playerVelocity.y * Vector3.up);
        }

        controller.Move(finalMove * Time.deltaTime);
        combatControls();
        updateAnimations();
    }


    private void combatControls()
    {
        if (lockOnTargetAction.action.triggered)
        {
            if (!lockedOnTarget) //initiating lockTarget mode, find target
            {
                GameObject target = FindBestLockOnTarget();
                if(target != null)
                {
                    currentTarget = target;
                    lockedOnTarget = true;
                    lastSwitchTime = Time.time;
                }
            }
            else //release lockTarget mode
            {
                lockedOnTarget = false;
                currentTarget = null;
            }
        }

        if (AttackAction.action.triggered && !isAttacking)
        {
            anim.SetBool("IsAttacking", true);
            isAttacking = true;
        }

        if (rollAction.action.triggered && !isRolling && grounded && !isAttacking)
        {
            StartRoll();
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        inputsLocked = true;
        anim.SetBool("IsRolling", true);

        if (moveDir.magnitude > 0.1f)
        {
            rollDirection = moveDir;
        }
        else
        {
            rollDirection = playerObj.transform.forward;
        }

        //  set rotation to roll
        rollTargetRotation = Quaternion.LookRotation(rollDirection, Vector3.up);
        //anim.SetTrigger("Roll");
    }

    private GameObject FindBestLockOnTarget()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, targetLockRange, lockTarget);
        GameObject bestTarget = null;
        float bestScore = float.MaxValue;

        if (potentialTargets.Length == 0) return null;

        //get camera directions
        Vector3 cameraForward = orientation.transform.forward;
        Vector3 cameraPosition = orientation.transform.position;


        foreach (Collider targetCollider in potentialTargets)
        {
            //skip self
            if (targetCollider.gameObject == gameObject) continue;

            Vector3 targetPos = targetCollider.bounds.center;
            Vector3 directionToTarget = targetPos - orientation.position;

            float dot = Vector3.Dot(cameraForward.normalized, directionToTarget.normalized);
            if (dot < 0.1f) continue; // if the object is *behind* player, skip it, dont lock to it
            //not technically behind, dot < 0.1 means it is more than ~80deg to the left and right of the player

            //Calculate angle from player cam center to the target and also distance
            float angle = Vector3.Angle(cameraForward, directionToTarget);
            float distance = Vector3.Distance(transform.position, targetPos);

            //use angle and distance to create a score, lower score means priority to be locked
            float score = angle + (distance * 0.1f);
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = targetCollider.gameObject;
            }
        }

        return bestTarget;
    }

    private void CheckForTargetSwitch()
    {
        if (currentTarget == null || !lockedOnTarget) return;
        if (Time.time < lastSwitchTime + targetSwitchCooldown) return;

        Vector2 inputDelta = rotateController.combinedDelta;

        if (inputDelta.magnitude < targetSwitchThreshold) return;

        Vector2 inputDir = inputDelta.normalized;

        //get all possible lock target 
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, targetLockRange, lockTarget);

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        GameObject bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider target in potentialTargets)
        {
            if (target.gameObject == currentTarget || target.gameObject == gameObject) continue;

            Vector3 worldPos = target.transform.position;
            Vector3 screenPoint = cam.WorldToScreenPoint(worldPos);

            if (screenPoint.z < 0) continue;

            Vector2 screenPos = new Vector2(screenPoint.x,screenPoint.y);
            Vector2 disFromCenter = screenPos - screenCenter;
            float dot = Vector2.Dot(inputDir.normalized, disFromCenter.normalized);

            if (dot < 0.8f) continue;

            float dis3D = Vector3.Distance(cam.transform.position, worldPos);

            float score = disFromCenter.magnitude + (dis3D * 0.1f);

            if(score < bestScore)
            {
                bestScore = score;
                bestTarget = target.gameObject;
            }
        }

        if(bestTarget != null)
        {
            currentTarget = bestTarget;
            lastSwitchTime = Time.time;
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
