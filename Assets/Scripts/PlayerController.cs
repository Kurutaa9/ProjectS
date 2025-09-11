using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float playerSpeed;
    public float jumpHeight;
    public float rotationSpeed;

    [Header("Input")]
    public PlayerRotateController rotateController;
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

        //set the player facing direction (only when in freelook) otherwise lock the camera to target
        if (lockedOnTarget)
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

        GameObject bestTarget = null;
        float bestScore = float.MaxValue;

        //direction from camera to current Target
        Vector3 currentTargetDir = currentTarget.transform.position - orientation.position;
        Vector3 currentTargetDirFlat = new Vector3(currentTargetDir.x, 0, currentTargetDir.z).normalized;

        Vector3 forward = orientation.forward;
        Vector3 right = orientation.right;
        forward.y = 0;
        forward = forward.normalized;

        Vector3 desiredLookDir = forward * inputDir.y + right * inputDir.x;
        desiredLookDir = desiredLookDir.normalized;


        foreach (Collider target in potentialTargets)
        {
            if (target.gameObject == currentTarget || target.gameObject == gameObject) continue;

            Vector3 dirToTarget = target.transform.position - orientation.position;
            //Vector3 dirToTargetFlat = new Vector3(dirToTarget.x, 0, dirToTarget.z);
            float distanceToTarget = dirToTarget.magnitude;
            //dirToTargetFlat = dirToTargetFlat.normalized;
            float dotProduct = Vector3.Dot(currentTargetDirFlat, dirToTarget);

            if (dotProduct > 0)
            {
                float heightDifference = dirToTarget.y - currentTargetDir.y;

                bool matchesVertical = (inputDir.y > 0 && heightDifference > 0) ||
                                      (inputDir.y < 0 && heightDifference < 0) ||
                                      (Mathf.Abs(inputDir.y) < 0.1f);

                if (matchesVertical)
                {
                    //angle between desired direction and direction to this target
                    float angle = Vector3.Angle(desiredLookDir, dirToTarget);
                    float score = angle + (distanceToTarget * 0.1f);


                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTarget = target.gameObject;
                    }
                }
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
