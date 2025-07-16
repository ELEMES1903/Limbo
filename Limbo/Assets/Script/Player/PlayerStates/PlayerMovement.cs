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

    void LateUpdate()
    {
        CheckLedgeDetection();
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
    

    private bool wallDetectedLastFrame = false;
    private bool ledgeDetectedLastFrame = false;
    public float ledgeCheckHeight = 1.5f;
    public float ledgeRayDistance = 1f;
    public LayerMask wallLayer;
    public void CheckLedgeDetection()
    {
        Vector3 origin = cameraHolder.position;
        Vector3 direction = cameraHolder.forward;

        // Forward ray to detect wall
        if (Physics.Raycast(origin, direction, out RaycastHit wallHit, ledgeRayDistance, wallLayer))
        {
            //checks if hit normal if vertical(wall), regardless of Y rotation
            if (Mathf.Abs(Vector3.Dot(wallHit.normal, Vector3.up)) < 0.2f)
            {
                if (!wallDetectedLastFrame)
                {
                    Debug.Log("Wall detected");
                    DrawDebugSphere(wallHit.point, Color.blue, 1f); // wall point
                    wallDetectedLastFrame = true;
                }

                // Offset ledge check origin slightly in wall direction
                Vector3 ledgeCheckOrigin = wallHit.point + direction.normalized * 0.1f + Vector3.up * ledgeCheckHeight;

                if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit floorHit, ledgeCheckHeight, wallLayer))
                {
                    //check if hit normal is up (floor) and raycast start inside of wall
                    if (floorHit.distance > 0.05f && Vector3.Dot(floorHit.normal, Vector3.up) > 0.8f)
                    {
                        if (!ledgeDetectedLastFrame)
                        {
                            Debug.Log("Ledge detected");
                            DrawDebugSphere(floorHit.point, Color.yellow, 1f); // ledge point
                            ledgeDetectedLastFrame = true;
                        }
                    }
                }
                else
                {
                    ledgeDetectedLastFrame = false;
                }
            }
        }
            else
            {
                if (wallDetectedLastFrame)
                {
                    Debug.Log("Wall no longer detected");
                    wallDetectedLastFrame = false;
                }

                ledgeDetectedLastFrame = false;
            }

        // Optional: visualize rays
        Debug.DrawRay(origin, direction * ledgeRayDistance, Color.red);
    }
    public GameObject debugSpherePrefab;

    void DrawDebugSphere(Vector3 position, Color color, float duration = 1f, float size = 0.5f)
    {
        if (debugSpherePrefab == null)
        {
            Debug.LogWarning("No debugSpherePrefab assigned.");
            return;
        }

        GameObject sphere = Instantiate(debugSpherePrefab, position, Quaternion.identity);
        sphere.transform.localScale = Vector3.one * size;

        var renderer = sphere.GetComponent<Renderer>();
        if (renderer != null) renderer.material.color = color;

        Destroy(sphere, duration);
    }

}
