using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotateController : MonoBehaviour
{
    
    public float sensx;
    public float sensy;
    public float returnToTargetSpeed;
    public float mouseIdleThreshold;
    public float returnDelay;
    public float maxAngleOffset;

    [Header("Controller Input")]
    public InputActionReference lookAction;
    public float controllerSensitivity;


    float yRotation;
    float xRotation;

    public PlayerController playerController;

    private float mouseIdleTimer;
    private bool isManuallyControlling = false;
    private Vector2 combinedDelta;
    private Vector2 mouseDelta;
    private Vector2 controllerDelta;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        lookAction.action.Enable();

        combinedDelta = Vector2.zero;
        mouseDelta = Vector2.zero;
        controllerDelta = Vector2.zero;
    }

    void OnDisable()
    {
        lookAction.action.Disable();
    }

    void Update()
    {
        if(Mouse.current != null){
            //mouse input for rotation
            mouseDelta = Mouse.current.delta.ReadValue();
            mouseDelta.x = mouseDelta.x * sensx * Time.deltaTime;
            mouseDelta.y = mouseDelta.y * sensy * Time.deltaTime;
        }

        if(Gamepad.current != null){
            //controller input
            controllerDelta = Gamepad.current.rightStick.ReadValue() * controllerSensitivity * Time.deltaTime;
        }

        combinedDelta = mouseDelta + controllerDelta;

        if (playerController.lockedOnTarget)
        {
            if(combinedDelta.magnitude > mouseIdleThreshold)
            {
                isManuallyControlling = true;
                mouseIdleTimer = 0f;
                
                Vector3 targetDirection = playerController.currentTarget.transform.position - transform.position;
                targetDirection.y = 0;
                Quaternion idealRotation = Quaternion.LookRotation(targetDirection);
                float idealYRotation = idealRotation.eulerAngles.y;
                
                float lookx = combinedDelta.x;
                float looky = combinedDelta.y;

                //check if the mouse movement will exceed the specified maxangle when locked on to a target
                //if it is only allow movement inward and not outward else normal mouse movement
                float newYRotation = yRotation + lookx;
                float newAngleFromTarget = Mathf.DeltaAngle(newYRotation, idealYRotation);
                if (Mathf.Abs(newAngleFromTarget) <= maxAngleOffset)
                {
                    yRotation = newYRotation;
                }
                else
                {
                    //if the mouse is moving towards the center(inwards)
                    if (Mathf.Abs(newAngleFromTarget) < Mathf.Abs(Mathf.DeltaAngle(yRotation, idealYRotation)))
                    {
                        yRotation = newYRotation;
                    }
                    else // otherwise clamp it so it doesnt move futher
                    {
                        float sign = Mathf.Sign(Mathf.DeltaAngle(yRotation, idealYRotation));
                        yRotation = idealYRotation + (-sign * maxAngleOffset);
                    }
                }

                xRotation -= looky;
                xRotation = ClampAngle(xRotation, -80f, 80f);

                transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            }
            else if (isManuallyControlling)
            {
                mouseIdleTimer += Time.deltaTime;
                Debug.Log(mouseIdleTimer);
                //if the player isnt controlling the mouse anymore for a while, go back to face the target
                if(mouseIdleTimer > returnDelay)
                {
                    Vector3 targetDirection = playerController.currentTarget.transform.position - transform.position;
                    targetDirection.y = 0;
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                    float targetX = targetRotation.eulerAngles.x;
                    if (targetX > 180f) targetX -= 360f;
                    targetX = ClampAngle(targetX, -80f, 80f);

                    float targetY = targetRotation.eulerAngles.y;

                    xRotation = Mathf.LerpAngle(xRotation, targetX, returnToTargetSpeed * Time.deltaTime);
                    yRotation = Mathf.LerpAngle(yRotation, targetY, returnToTargetSpeed * Time.deltaTime);

                    transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);

                    if (xRotation == targetX && yRotation == targetY)
                    {
                        isManuallyControlling = false;
                    }
                }
            }
            else
            {
                Vector3 targetDirection = playerController.currentTarget.transform.position - transform.position;
                targetDirection.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

                float targetX = targetRotation.eulerAngles.x;
                if (targetX > 180f) targetX -= 360f;
                targetX = ClampAngle(targetX, -80f, 80f);

                xRotation = targetX;
                yRotation = targetRotation.eulerAngles.y;

                Quaternion finalRotation = Quaternion.Euler(xRotation, yRotation, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, playerController.rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            float lookx = combinedDelta.x;
            float looky = combinedDelta.y;

            yRotation += lookx;
            xRotation -= looky;
            xRotation = ClampAngle(xRotation, -80f, 80f);

            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        }
    }

    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
}
