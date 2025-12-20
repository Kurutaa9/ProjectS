using System.Collections;
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
    public InputActionReference heavyAttackAction;
    public InputActionReference sprintAction;
    public InputActionReference rollAction;
    public InputActionReference lockOnTargetAction;
    public InputActionReference healAction;

    [Header("orientation")]
    public Camera cam;
    public Transform orientation;

    [Header("Object")]
    public GameObject playerObj;
    [Header("VFX")]
    public GameObject healVFX;
    public Vector3 healVFXOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Animation")]
    public Animator anim;
    private RuntimeAnimatorController baseAnimatorController;

    [Header("Layer masks")]
    public LayerMask ground;
    public LayerMask lockTarget;

    [Header("Player Controller")]
    public CharacterController controller;
    public PlayerStats playerStats;

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

    [Header("Audio")]
    public AudioSource audioSource;
    public System.Collections.Generic.List<PlayerStats.SoundEffect> rollSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();
    public System.Collections.Generic.List<PlayerStats.SoundEffect> restEnterSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();
    public System.Collections.Generic.List<PlayerStats.SoundEffect> restExitSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();
    public System.Collections.Generic.List<PlayerStats.SoundEffect> walkSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();
    public System.Collections.Generic.List<PlayerStats.SoundEffect> runSounds = new System.Collections.Generic.List<PlayerStats.SoundEffect>();
    public float footstepIntervalWalk = 0.5f;
    public float footstepIntervalRun = 0.3f;
    private float footstepTimer = 0f;

    // Sprinting
    [Header("Sprint Settings")]
    public bool isSprinting = false;
    [Tooltip("Multiplier applied to playerSpeed while sprinting")]
    public float sprintSpeedMultiplier = 1.75f;
    [Tooltip("Stamina drained per second while sprinting")]
    public float sprintStaminaDrain = 10f;
    [Tooltip("Stamina cost applied once when jumping")]
    public float jumpStaminaCost = 15f;
    [Header("Exhaustion")]
    [Tooltip("If stamina hits zero while sprinting the player becomes exhausted and cannot sprint again until stamina recovers to this percent of max (0-1)")]
    public float exhaustionRecoverPercent = 0.2f;
    public bool isExhausted = false;

    //Attacking
    [Header("Combat Settings")]
    [Tooltip("Normalized time (0-1) of the attack animation after which input buffering is allowed.")]
    public float attackBufferWindow = 0.7f;
    public PlayerCombat playerCombat;
    public bool IsAttacking = false;
    public bool attackLocked = false;

    public bool isTakingHit = false;
    public bool isHealing = false;

    private void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (lockOnTargetAction != null) lockOnTargetAction.action.Enable();
        if (AttackAction != null) AttackAction.action.Enable();
        if (heavyAttackAction != null) heavyAttackAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (rollAction != null) rollAction.action.Enable();
        if (healAction != null) healAction.action.Enable();

        baseAnimatorController = anim.runtimeAnimatorController;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        jumpAction.action.Disable();
        lockOnTargetAction.action.Disable();
        AttackAction.action.Disable();
        sprintAction.action.Disable();
        rollAction.action.Disable();
        healAction.action.Disable();
    }

    void Update()
    {
        // Prevent stamina regen while attacking
        if (IsAttacking && playerStats != null)
        {
            playerStats.ResetRegenTimer();
        }

        // SphereCast for better ground detection on uneven terrain
        float radius = controller.radius * 0.9f;
        float castDistance = (controller.height / 2f) - radius + 0.2f;
        grounded = Physics.SphereCast(transform.position + controller.center, radius, Vector3.down, out _, castDistance, ground);
        Debug.Log(grounded);
        //if player hits ground and is falling, stop falling...
        if (grounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
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
        if (jumpAction.action.triggered && grounded && !inputsLocked)
        {
            if (playerStats != null && playerStats.CanPerformAction(jumpStaminaCost))
            {
                playerStats.ConsumeStamina(jumpStaminaCost);
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            }
            // else
            // {
            //     // Not enough stamina — do not jump. Could add feedback here.
            // }
        }

        // Apply gravity
        playerVelocity.y += gravity * Time.deltaTime;

        // Recover from exhaustion when stamina has regenerated enough
        if (isExhausted && playerStats != null)
        {
            if (playerStats.GetCurrentStamina() >= playerStats.GetMaxStamina() * exhaustionRecoverPercent)
            {
                isExhausted = false;
            }
        }

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
            if(currentTarget == null || !currentTarget || !currentTarget.activeInHierarchy)
            {
                lockedOnTarget = false;
                currentTarget = null;
            }
            else
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

        // Determine whether sprinting is active: sprint only when moving, grounded,
        // not target-locked, not rolling, and inputs aren't locked.
        float speedMultiplier = 1f;
        try
        {
            float sprintVal = (sprintAction != null) ? sprintAction.action.ReadValue<float>() : 0f;
            // Can't start sprint if exhausted
            isSprinting = sprintVal > 0.5f && !lockedOnTarget && moveDir.magnitude > 0.1f && !inputsLocked && grounded && !isRolling && !isExhausted;
        }
        catch
        {
            // In case the action isn't set or ReadValue fails, ensure sprint is false
            isSprinting = false;
        }

        if (isSprinting)
        {
            // Drain stamina over time while sprinting. If stamina runs out, stop sprinting
            // and set exhaustion. We drain up to the remaining stamina so stamina will
            // not go negative.
            float drain = sprintStaminaDrain * Time.deltaTime;
            if (playerStats != null)
            {
                float current = playerStats.GetCurrentStamina();
                float actualDrain = Mathf.Min(drain, current);
                if (actualDrain > 0f)
                {
                    playerStats.ConsumeStamina(actualDrain);
                }

                if (current <= drain)
                {
                    // stamina would reach zero this frame -> exhaustion
                    isSprinting = false;
                    isExhausted = true;
                    speedMultiplier = 1f;
                }
                else
                {
                    speedMultiplier = sprintSpeedMultiplier;
                }
            }
            else
            {
                speedMultiplier = sprintSpeedMultiplier;
            }
        }

        if (isTakingHit)
        {
            move = Vector3.zero;
            moveDir = Vector3.zero;
        }

        if (isRolling)
        {
            finalMove = (rollDirection * playerSpeed * rollSpeedMultiplier) + (playerVelocity.y * Vector3.up);
        }
        else
        {
            finalMove = (moveDir * playerSpeed * speedMultiplier) + (playerVelocity.y * Vector3.up);
        }

        

        controller.Move(finalMove * Time.deltaTime);
        HandleFootsteps();
        combatControls();
        updateAnimations();
    }

    private void HandleFootsteps()
    {
        if (grounded && !inputsLocked && !isRolling && !isTakingHit && move.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                if (isSprinting)
                {
                    PlaySounds(runSounds);
                    footstepTimer = footstepIntervalRun;
                }
                else
                {
                    PlaySounds(walkSounds);
                    footstepTimer = footstepIntervalWalk;
                }
            }
        }
        else
        {
            footstepTimer = Mathf.Min(footstepTimer, 0.05f);
        }
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

        //attacking
        if (AttackAction.action.triggered && !attackLocked)
        {
            if (CanBufferAttack())
            {
                playerCombat.attackbuffer = true;
            }
        }

        //heavy attack
        if (heavyAttackAction != null && heavyAttackAction.action.triggered && !attackLocked)
        {
            if (CanBufferAttack())
            {
                playerCombat.heavyAttackBuffer = true;
            }
        }

        //healing
        if (healAction.action.triggered && !isHealing && grounded && !inputsLocked && !IsAttacking && !isRolling)
        {
            if (playerStats != null && playerStats.CanHeal())
            {
                StartCoroutine(HealRoutine());
            }
        }

        //roll
        if (rollAction.action.triggered && !isRolling && grounded && !inputsLocked && playerStats.CanPerformAction(playerStats.GetRollStaminaCost()))
        {
            playerStats.ConsumeStamina(playerStats.GetRollStaminaCost());
            StartRoll();
        }
    }

    private bool CanBufferAttack()
    {
        // If we are currently attacking, check if we are far enough into the animation
        if (IsAttacking && anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime < attackBufferWindow)
            {
                return false;
            }
        }
        return true;
    }

    private void StartRoll()
    {
        isRolling = true;
        inputsLocked = true;
        attackLocked = true;
        PlaySounds(rollSounds);

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
        anim.SetBool("IsRolling", true);
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
        if (isTakingHit)
        {
            anim.SetBool("lockedOnTarget", lockedOnTarget);
            anim.SetBool("isSprinting", false);
            anim.SetBool("walkForward", false);
            return;
        }

        anim.SetBool("lockedOnTarget", lockedOnTarget);
        anim.SetBool("isSprinting", isSprinting);
        if (lockedOnTarget)
        {
            Vector3 localVelocity = playerObj.transform.InverseTransformDirection(controller.velocity);
            anim.SetFloat("strafevelx", localVelocity.x, 0.2f, Time.deltaTime);
            anim.SetFloat("strafevely", localVelocity.z, 0.2f, Time.deltaTime);
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

    public void OnHit()
    {
        if (isTakingHit) return;

        if (playerCombat != null && IsAttacking)
        {
            playerCombat.InterruptCombo();
        }

        // Clear ongoing actions
        isRolling = false;
        isSprinting = false;
        IsAttacking = false;
        attackLocked = true;
        inputsLocked = true;
        isTakingHit = true;

        // Immediately play GetHit
        anim.Play("getHit", 0, 0f);

        // Stop movement
        playerVelocity = Vector3.zero;

        // Run a coroutine to restore control after the animation ends
        StartCoroutine(RestoreAfterGetHit());
    }

    private IEnumerator RestoreAfterGetHit()
    {
        // Wait until we enter the GetHit state
        while (!IsInState(anim, "getHit")) yield return null;
        // Then wait until it finishes
        while (IsInState(anim, "getHit")) yield return null;

        // Restore control; Animator transitions to Idle via your state machine
        inputsLocked = false;
        attackLocked = false;
        isTakingHit = false;
    }

    private IEnumerator HealRoutine()
    {
        isHealing = true;
        inputsLocked = true;
        attackLocked = true;
        anim.Play("Heal", 0, 0f);

        // Wait for transition to Heal state
        yield return new WaitForSeconds(0.1f);

        // Wait until we are in "Heal" state
        float timeout = 0f;
        while (!IsInState(anim, "Heal") && timeout < 1f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        if (timeout >= 1f)
        {
            Debug.LogWarning("Heal animation state not found or took too long.");
            isHealing = false;
            inputsLocked = false;
            attackLocked = false;
            yield break;
        }

        bool healed = false;
        while (IsInState(anim, "Heal"))
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            if (!healed && info.normalizedTime >= 0.5f)
            {
                if (playerStats != null)
                {
                    playerStats.Heal(playerStats.healAmount);
                    playerStats.ConsumeFlask();
                }

                if (healVFX != null)
                {
                    GameObject vfx = Instantiate(healVFX, transform.position + healVFXOffset, Quaternion.identity);
                    ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        Destroy(vfx, ps.main.duration + ps.main.startLifetime.constantMax);
                    }
                    else
                    {
                        Destroy(vfx, 2f);
                    }
                }

                healed = true;
            }
            yield return null;
        }

        if (!healed)
        {
            if (playerStats != null)
                playerStats.Heal(playerStats.healAmount);
        }

        isHealing = false;
        inputsLocked = false;
        attackLocked = false;
    }

    private bool IsInState(Animator a, string stateName)
    {
        AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(0);
        return info.IsName(stateName);
    }

    public void HandlePlayerDeath()
    {
        StopAllCoroutines();
        
        if (playerCombat != null)
        {
            playerCombat.InterruptCombo();
        }
        if (anim != null && baseAnimatorController != null)
        {
            anim.runtimeAnimatorController = baseAnimatorController;
        }

        inputsLocked = true;
        attackLocked = true;
        
        isTakingHit = false;
        isRolling = false;
        isSprinting = false;
        IsAttacking = false;
        isHealing = false;
        lockedOnTarget = false;
        currentTarget = null;

        playerVelocity = Vector3.zero;
        move = Vector3.zero;
        moveDir = Vector3.zero;

    }

    public void RespawnPlayer()
    {
        inputsLocked = false;
        attackLocked = false;
        isTakingHit = false;
        isRolling = false;
        isSprinting = false;
        IsAttacking = false;
        isHealing = false;
        lockedOnTarget = false;
        currentTarget = null;

        playerVelocity = Vector3.zero;
        move = Vector3.zero;
        moveDir = Vector3.zero;

        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // Respawn all enemies
        EnemyRespawnManager.RespawnAllEnemies();
    }

    public void RestAtSaveSpot()
    {
        // 1. Reset Player Stats (Full Health/Stamina)
        if (playerStats != null)
        {
            playerStats.ResetStats();
        }

        // 2. Reset Inputs/States (Just in case)
        inputsLocked = false;
        attackLocked = false;
        isTakingHit = false;
        isExhausted = false;
        IsAttacking = false;
        
        // 3. Respawn/Reset all enemies
        EnemyRespawnManager.RespawnAllEnemies();

        Debug.Log("Rested at save spot. Enemies respawned and stats reset.");
    }

    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay, float volume)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private void PlaySounds(System.Collections.Generic.List<PlayerStats.SoundEffect> sounds)
    {
        if (audioSource == null || sounds == null) return;
        foreach (var sfx in sounds)
        {
            if (sfx.clip != null)
            {
                if (sfx.delay > 0)
                    StartCoroutine(PlaySoundDelayed(sfx.clip, sfx.delay, sfx.volume));
                else
                    audioSource.PlayOneShot(sfx.clip, sfx.volume);
            }
        }
    }

    public void PlayRestEnterSound() { PlaySounds(restEnterSounds); }
    public void PlayRestExitSound() { PlaySounds(restExitSounds); }
}
