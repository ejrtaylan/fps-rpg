using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public float mouseXSensitivity = 100f;
    public float mouseYSensitivity = 100f;

    public Vector2 moveInput;
    public Vector2 mouseLookInput;

    //public bool jumpPressed = false;
    public float jumpBufferTime = 0.1f;
    private float jumpTimer;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            jumpTimer = jumpBufferTime;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        mouseLookInput = context.ReadValue<Vector2>();
    }

    public bool ConsumeJump()
    {
        if (jumpTimer > 0)
        {
            jumpTimer = 0;
            return true;
        }
        return false;
    }

    private void Update()
    {
        jumpTimer = Mathf.Max(jumpTimer - Time.deltaTime, 0f);
    }

}

    
