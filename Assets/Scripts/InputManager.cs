using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private InputActionAsset inputActions;

    public float mouseXSensitivity = 100f;
    public float mouseYSensitivity = 100f;

    public Vector2 moveInput;
    public Vector2 mouseLookInput;

    //public bool jumpPressed = false;
    public float jumpBufferTime = 0.1f;
    private float jumpTimer;

    private void Awake()
    {
        if(inputActions == null) inputActions = GetComponent<PlayerInput>().actions;
    }
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        //Unity said to disable all action maps on start idk
        for(int i = 0; i < inputActions.actionMaps.Count; i++)
        {
            var actionMap = inputActions.actionMaps[i];
            actionMap.Disable();
        }

        //Enable player action map so we can do shit
        inputActions.FindActionMap("Player").Enable();
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

    
