using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllerScript : MonoBehaviour
{

    [SerializeField] private Camera cam;
    [SerializeField] private CharacterController controller;
    [SerializeField] private InputManager inputManager;

    [SerializeField] float gravity = -9.0f;
    private Vector3 velocity;
    public float speed = 5f;
    public float jumpHeight = 1.5f;

    public float camXRotation = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (inputManager == null) inputManager = GetComponent<InputManager>();
    }

    void Update()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = transform.TransformDirection(new Vector3(inputManager.moveInput.x, 0, inputManager.moveInput.y)) * speed;

        //Jump
        if (inputManager.ConsumeJump() && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        //Fall down
        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move + velocity;
        controller.Move(finalMove * Time.deltaTime);
    }

    private void LateUpdate()
    {
        camXRotation -= (inputManager.mouseLookInput.y * Time.deltaTime) * inputManager.mouseYSensitivity;
        camXRotation = Mathf.Clamp(camXRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(camXRotation, 0, 0);

        transform.Rotate(Vector3.up * (inputManager.mouseLookInput.x * Time.deltaTime) * inputManager.mouseXSensitivity);
    }

}
