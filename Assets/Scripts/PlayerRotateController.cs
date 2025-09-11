using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotateController : MonoBehaviour
{
    
    public float sensx;
    public float sensy;

    [Header("Controller Input")]
    public InputActionReference lookAction;
    public float controllerSensitivity;


    float yRotation;
    float xRotation;

    public PlayerController playerController;

    //INPUTS / KEYBINDS VARIABLES
    public Vector2 combinedDelta;
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
            Vector3 targetDirection = playerController.currentTarget.transform.position - transform.position;
            targetDirection.y -= 2f;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            float targetX = targetRotation.eulerAngles.x;
            if (targetX > 180f) targetX -= 360f;
            targetX = ClampAngle(targetX, -80f, 80f);

            xRotation = targetX;
            yRotation = targetRotation.eulerAngles.y;

            Quaternion finalRotation = Quaternion.Euler(xRotation, yRotation, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, playerController.rotationSpeed * Time.deltaTime);
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
