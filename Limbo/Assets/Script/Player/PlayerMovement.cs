using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Run/Sprint Multipliers")]
    public float runMultiplier = 1.3f;
    public float sprintMultiplier = 1.8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraHolder;
    private float pitch = 0f;

    [Header("Gravity & Jump")]
    public float gravity = -9.81f;
    public float jumpHeight = 2.5f;
    private float verticalVelocity = 0f;

    private CharacterController controller;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 inputDirection = Vector3.zero;

    public enum MovementMode { Idle, Walk, Run, Sprint }
    public MovementMode currentMovementMode = MovementMode.Idle;
    private CharacterManager characterManager;
    public Animator animator;

    public enum PlayerState
    {
        Normal,
        Hanging,
        Climbing
    }
    public PlayerState currentState = PlayerState.Normal;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        characterManager = GetComponent<CharacterManager>();
        animator = GetComponentInChildren<Animator>();
        Cursor.lockState = CursorLockMode.None;
        
    }

    void Update()
    {
        Look();
        // Skip movement if hanging
        if (currentState == PlayerState.Hanging)
            return;
        HandleMovementInput();
        HandleMovementMode();
        ApplyGravityAndJump();
        MovePlayer();
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0f, vertical).normalized;

        // Get directions relative to camera
        Vector3 camForward = cameraHolder.forward;
        Vector3 camRight = cameraHolder.right;
        camForward.y = 0f;
        camRight.y = 0f;

        inputDirection = (camForward.normalized * input.z + camRight.normalized * input.x).normalized;
    }

    void HandleMovementMode()
    {
        currentMovementMode = MovementMode.Idle;

        bool isMoving = inputDirection.magnitude > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftControl);
        bool isRunning = characterManager.IsRunning() && characterManager.canRun;

        if (isSprinting)
            currentMovementMode = MovementMode.Sprint;
        else if (isRunning)
            currentMovementMode = MovementMode.Run;
        else if (isMoving)
            currentMovementMode = MovementMode.Walk;

        // Animation states (optional in first-person)
        animator?.SetBool("isWalking", currentMovementMode == MovementMode.Walk);
        animator?.SetBool("isRunning", currentMovementMode == MovementMode.Run);
    }

    void ApplyGravityAndJump()
    {
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -1f;

            if (Input.GetButtonDown("Jump"))
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    void MovePlayer()
    {
        float multiplier = 1f;
        switch (currentMovementMode)
        {
            case MovementMode.Run: multiplier = runMultiplier; break;
            case MovementMode.Sprint: multiplier = sprintMultiplier; break;
        }

        float targetSpeed = inputDirection.magnitude > 0.1f ? moveSpeed * multiplier : 0f;
        Vector3 targetVelocity = inputDirection * targetSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * (targetSpeed > currentVelocity.magnitude ? acceleration : deceleration));

        Vector3 move = currentVelocity + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }
}
