using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotateController : MonoBehaviour
{
    
    public float sensx;
    
    public float sensy;

    float yRotation;
    float xRotation;


    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        //mouse input for rotation
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mousex = mouseDelta.x * sensx;
        float mousey = mouseDelta.y * sensy;

        yRotation += mousex;

        xRotation -= mousey;
        xRotation = ClampAngle(xRotation, -80f, 80f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
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
