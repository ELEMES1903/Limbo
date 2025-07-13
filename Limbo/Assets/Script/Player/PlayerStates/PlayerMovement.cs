using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerStateManager : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform cameraHolder;

    [Header("Jump & Gravity")]
    public float gravity = -9.81f;
    public float jumpHeight = 2.5f;
    [HideInInspector] public float verticalVelocity = 0f;

    [Header("Debug")]
    public string currentStateName;

    [HideInInspector] public Vector3 inputDirection = Vector3.zero;
    [HideInInspector] public CharacterController controller;

    private float pitch = 0f;
    private PlayerStateBase currentState;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        SwitchState(new IdleState(this));
    }

    void Update()
    {  
        HandleTransitions();
        currentState.UpdateState();
        currentStateName = currentState.GetType().Name;
    }

    public void SwitchState(PlayerStateBase newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    void HandleTransitions()
    {
        if (!controller.isGrounded)
            return;

        if (inputDirection.magnitude <= 0.1f)
        {
            if (!(currentState is IdleState))
                SwitchState(new IdleState(this));
        }
        else
        {
            if (!(currentState is WalkingState))
                SwitchState(new WalkingState(this));
        }
    }

    public void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        cameraHolder.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

        Vector3 camForward = cameraHolder.forward;
        Vector3 camRight = cameraHolder.right;
        camForward.y = 0;
        camRight.y = 0;

        inputDirection = (camForward.normalized * input.z + camRight.normalized * input.x).normalized;
    }

    public void MovePlayer(float targetSpeed)
    {
        Vector3 targetVelocity = inputDirection * targetSpeed;
        Vector3 currentVelocity = Vector3.Lerp(Vector3.zero, targetVelocity, Time.deltaTime * (targetSpeed > 0 ? acceleration : deceleration));
        Vector3 move = currentVelocity + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    public void ApplyGravityAndJump()
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
}
