using UnityEngine;
using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine.InputSystem;

public class PlayerStateManager : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;
    
    [Header("InputAction")]
    [HideInInspector] public PlayerInputActions playerInputActions;
    [HideInInspector] public InputActionMap currentActionMap;

    [Header("Movement Settings")]
    public float maxSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    private Vector3 inputDirection = Vector3.zero;
    private Vector2 moveInputValue;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform cameraHolder;
    private float pitch = 0f;
    private float yaw = 0f;
    [HideInInspector] public float initialYaw;
    public bool rotatePlayerToCamera;
    public float previousPlayerYaw;

    [Header("Jump & Gravity")]
    public Vector3 gravityDirection = Vector3.down;
    public float jumpForce = 7f;
    public float gravityStrength;
    [HideInInspector] public bool jumpInputDetected;
    private bool isGrounded;

    [Header("State")]
    public string currentStateName;
    private PlayerStateBase currentState;
    public bool inState;

    [Header("Ledge Detection")]
    public float ledgeCheckHeight = 0.5f;
    public float ledgeRayDistance = 1f;
    public Transform ledgeRaycastOrigin;

    [Header("Rope")]
    public float ropeJumpForce = 1f;
    public SplineFollower splineFollower;
    public SplineComputer splineComputer;
    [HideInInspector] public Vector3 ropeContactPoint;
    public bool ropeDetected;


    [HideInInspector] public GarysVerletRope rope;

    [Header("Debug")]
    public float timesToResetVelocity = 10f;
    private Dictionary<string, float> timers = new Dictionary<string, float>();

    [Header("Layers")]
    public LayerMask ceilingLayer;
    public LayerMask floorLayer;
    public LayerMask wallLayer;

    [Header("Check Transforms")]

    public Transform groundCheck;
    public Transform ceilingCheck;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        splineFollower = transform.GetComponent<SplineFollower>();

        playerInputActions = new PlayerInputActions();

        //playerInputActions.OnGround.Jump.performed += DetectJumpInput;
        
        currentActionMap = playerInputActions.OnGround;
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SwitchState(new IdleState(this));
        rb.useGravity = false;
    }

    void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);

        HandleTransitions();
        
        currentState.UpdateState();
        currentStateName = currentState.GetType().Name;

        UpdateTimers(Time.deltaTime);
    }

    void FixedUpdate()
    {
        currentState.FixedUpdateState();
        Jump();
    }

    public void SwitchState(PlayerStateBase newState)
    {
        //quick fix logic for walk and idle state both enable/disable
        if (currentState is WalkingState || currentState is IdleState && newState is WalkingState || newState is IdleState) { }
        else if (currentState is WalkingState || currentState is IdleState) { playerInputActions.OnGround.Disable(); }
        else if (newState is WalkingState || newState is IdleState) { playerInputActions.OnGround.Enable(); }

        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();

        //buffer to reset velocity when entering states
        timesToResetVelocity = 10;
    }

    void HandleTransitions()
    {
        if (!(currentState is LedgeHangState) && IsTimerDone("LedgeHangCooldown") && CheckLedgeDetection(out Vector3 ledgePoint, out Vector3 wallNormal))
        {
            SwitchState(new LedgeHangState(this, ledgePoint, wallNormal));
            return;
        }

        if (!(currentState is RopeHangState) && IsTimerDone("RopeHangCooldown") && ropeDetected)
        {
            SwitchState(new RopeHangState(this));
            return;
        }

        if (inState){return;}

        if (!isGrounded && !(currentState is AirState))
        {
            SwitchState(new AirState(this));
            return;
        }

        if (!isGrounded){return;}

        if (inputDirection.magnitude <= 0.1f)
        {
            if (!(currentState is IdleState)) {SwitchState(new IdleState(this));}
        }
        else
        {
            if (!(currentState is WalkingState)) {SwitchState(new WalkingState(this));}
        }
    }
    
    public void CustomGravity()
    {
        Vector3 customGravity = gravityDirection.normalized * gravityStrength;
        rb.AddForce(customGravity, ForceMode.Acceleration);
    }

    public void DetectJumpInput(InputAction.CallbackContext context) { jumpInputDetected = true; }

    public void Jump()
    {
        if (jumpInputDetected && isGrounded)
        {
            // Remove existing velocity in gravity direction to get a consistent jump
            rb.linearVelocity -= Vector3.Project(rb.linearVelocity, -gravityDirection);

            // Apply jump force opposite to gravity
            rb.AddForce(-gravityDirection.normalized * jumpForce, ForceMode.VelocityChange);
        }
        jumpInputDetected = false; 
    }

    public void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        yaw += mouseX;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        if (currentState is LedgeHangState || currentState is RopeHangState)
        {
            // Get the current yaw of the player
            float currentPlayerYaw = transform.eulerAngles.y;

            // Calculate how much the player has rotated since the last frame
            float playerYawDelta = Mathf.DeltaAngle(previousPlayerYaw, currentPlayerYaw);

            // Add the same delta to the camera yaw
            yaw += playerYawDelta;

            // Clamp the yaw difference between camera and player
            float angleDifference = Mathf.DeltaAngle(currentPlayerYaw, yaw);
            float clampedAngle = Mathf.Clamp(angleDifference, -80f, 80f);
            yaw = currentPlayerYaw + clampedAngle;

            // Update previousPlayerYaw for the next frame
            previousPlayerYaw = currentPlayerYaw;
        }

        cameraHolder.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        cameraHolder.position = transform.position + 0.75f * Vector3.up;

        if (rotatePlayerToCamera)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, cameraHolder.eulerAngles.y, 0f);
            rb.MoveRotation(targetRotation);
        }
    }

    public void CalibrateCamera()
    {
        // Get the yaw (horizontal angle) of the player and camera
        float playerYaw = transform.eulerAngles.y;
        float cameraYaw = cameraHolder.eulerAngles.y;

        // Calculate the shortest signed angle between the player and camera yaw
        // This handles wraparound (e.g., 359° vs 1° becomes +2°, not -358°)
        // This value is stored to define the "initial" camera offset when entering ledgehang
        initialYaw = Mathf.DeltaAngle(playerYaw, cameraYaw);
    }

    public void HandleMovementInput()
    {
        moveInputValue = playerInputActions.OnGround.Move.ReadValue<Vector2>();
        Vector3 input = new Vector3(moveInputValue.x, 0, moveInputValue.y).normalized;

        Vector3 camForward = cameraHolder.forward;
        Vector3 camRight = cameraHolder.right;
        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        // Only calculate once per frame
        Vector3 moveDirection = (camForward * input.z + camRight * input.x).normalized;
        inputDirection = moveDirection;
    }

    public void Move()
    {
        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;

        float targetSpeed = maxSpeed;
        float speed = Mathf.Lerp(flatVelocity.magnitude, targetSpeed, Time.fixedDeltaTime * (targetSpeed > flatVelocity.magnitude ? acceleration : deceleration));
        Vector3 move = inputDirection * speed;
        move.y = rb.linearVelocity.y;

        rb.linearVelocity = move;
    }

    public bool CheckLedgeDetection(out Vector3 ledgePoint, out Vector3 wallNormal)
    {
        Vector3 origin = cameraHolder.position;
        Vector3 direction = cameraHolder.forward;
        ledgePoint = Vector3.zero;
        wallNormal = Vector3.zero;

        // Forward ray to detect wall
        if (Physics.Raycast(origin, direction, out RaycastHit wallHit, ledgeRayDistance, wallLayer))
        {
            // Check if wall is vertical
            if (Mathf.Abs(Vector3.Dot(wallHit.normal, Vector3.up)) < 0.2f)
            {
                wallNormal = wallHit.normal;

                // Offset ledge check origin slightly in wall direction
                Vector3 ledgeCheckOrigin = wallHit.point + direction.normalized * 0.1f + Vector3.up * ledgeCheckHeight;

                if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit floorHit, ledgeCheckHeight, wallLayer))
                {
                    // Confirm surface is walkable and we aren't inside geometry
                    if (floorHit.distance > 0.05f && Vector3.Dot(floorHit.normal, Vector3.up) > 0.8f)
                    {
                        ledgePoint = floorHit.point;
                        return true;
                    }
                }
            }
        }

        Vector3 mockLedgeCheckOrigin = origin + direction.normalized * ledgeRayDistance + direction.normalized * 0.1f + Vector3.up * ledgeCheckHeight;
        Debug.DrawRay(origin, direction * ledgeRayDistance, Color.red); //draw wall check ray
        Debug.DrawRay(mockLedgeCheckOrigin, Vector3.down * ledgeCheckHeight, Color.blue); //draw wall check ray
        return false;
    }

    public void ResetPlayerVelocity()
    {
        if (timesToResetVelocity > 0)
        {
            rb.linearVelocity = Vector3.zero;
            timesToResetVelocity--;
        }
    }

    private void UpdateTimers(float deltaTime)
    {
        var keys = new List<string>(timers.Keys);
        foreach (var key in keys)
        {
            timers[key] -= deltaTime;
            if (timers[key] <= 0f)
                timers.Remove(key);
        }
    }
    public void StartTimer(string timerName, float duration)
    {
        timers[timerName] = duration;

    }
    public bool IsTimerDone(string timerName)
    {
        return !timers.ContainsKey(timerName);
    }
}