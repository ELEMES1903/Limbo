using UnityEngine;
using System.Collections;

//[RequireComponent(typeof(CharacterController))]
public class PlayerStateManager : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    [Header("Movement Settings")]
    public float maxSpeed = 6f;
    public float acceleration = 10f;
    public float deceleration = 15f;
    [HideInInspector] public Vector3 inputDirection = Vector3.zero;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform cameraHolder;
    public float pitch = 0f;
    public float yaw = 0f;
    public float initialYaw;

    [Header("Jump & Gravity")]
    public Vector3 gravityDirection = Vector3.down;
    public  float jumpForce = 7f;
    public float gravityStrength;
    public bool inState;
    public bool isGrounded;

    [Header("State")]
    public string currentStateName;
    private PlayerStateBase currentState;

    [Header("Ledge Detection")]
    private bool wallDetectedLastFrame = false;
    private bool ledgeDetectedLastFrame = false;
    public float ledgeCheckHeight = 1.5f;
    public float ledgeRayDistance = 2f;
    public LayerMask wallLayer;
    public Transform ledgeRaycastOrigin;
    public bool exitingLedgeHang;

    [Header("Debug")]
    public GameObject debugSpherePrefab;

    public bool rotatePlayerToCamera;
    public float previousPlayerYaw;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        SwitchState(new IdleState(this));
        rb.useGravity = false; // Disable Unity's built-in gravity
    }

    void Update()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        HandleTransitions();
        currentState.UpdateState();
        currentStateName = currentState.GetType().Name;
    }

    void FixedUpdate()
    {
        currentState.FixedUpdateState();
    }

    public void CustomGravity()
    {
        Vector3 customGravity = gravityDirection.normalized * gravityStrength;
        rb.AddForce(customGravity, ForceMode.Acceleration);
    }
    public void SwitchState(PlayerStateBase newState)
    {
        currentState?.ExitState();
        currentState = newState;
        currentState.EnterState();
    }
    private void Jump()
    {
        // Remove existing velocity in gravity direction to get a consistent jump
        rb.linearVelocity -= Vector3.Project(rb.linearVelocity, -gravityDirection);

        // Apply jump force opposite to gravity
        rb.AddForce(-gravityDirection.normalized * jumpForce, ForceMode.VelocityChange);
    }
    void HandleTransitions()
    {
        if(exitingLedgeHang)
            StartCoroutine(exitLedgeHang(0.5f));

        if (!(currentState is LedgeHangState) && CheckLedgeDetection(out Vector3 ledgePoint, out Vector3 wallNormal) && !exitingLedgeHang)
        {
            SwitchState(new LedgeHangState(this, ledgePoint, wallNormal));
            return;
        }
        
        if (inState)
            return;
        
        if (!isGrounded && !(currentState is AirState))
        {
            SwitchState(new AirState(this));
            return;
        }
        
        if (!isGrounded)
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

    private IEnumerator exitLedgeHang(float duration)
    {
        yield return new WaitForSeconds(duration);
        exitingLedgeHang = false;
    }

    public void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch -= mouseY;
        yaw += mouseX;

        pitch = Mathf.Clamp(pitch, -80f, 80f);

        if (currentState is LedgeHangState)
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

    public void HandleMovementInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0, v).normalized;

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

    public void MovePlayer()
    {
        Vector3 flatVelocity = rb.linearVelocity;
        flatVelocity.y = 0f;

        float targetSpeed = maxSpeed;
        float speed = Mathf.Lerp(flatVelocity.magnitude, targetSpeed, Time.fixedDeltaTime * (targetSpeed > flatVelocity.magnitude ? acceleration : deceleration));
        Vector3 move = inputDirection * speed;
        move.y = rb.linearVelocity.y;

        rb.linearVelocity = move;

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            Jump();
        }
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
                if (!wallDetectedLastFrame)
                {
                    Debug.Log("Wall detected");
                    DrawDebugSphere(wallHit.point, Color.blue, 1f);
                    wallDetectedLastFrame = true;
                }

                // Offset ledge check origin slightly in wall direction
                Vector3 ledgeCheckOrigin = wallHit.point + direction.normalized * 0.1f + Vector3.up * ledgeCheckHeight;

                if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit floorHit, ledgeCheckHeight, wallLayer))
                {
                    // Confirm surface is walkable and we aren't inside geometry
                    if (floorHit.distance > 0.05f && Vector3.Dot(floorHit.normal, Vector3.up) > 0.8f)
                    {
                        if (!ledgeDetectedLastFrame)
                        {
                            Debug.Log("Ledge detected");
                            DrawDebugSphere(floorHit.point, Color.yellow, 1f);
                            ledgeDetectedLastFrame = true;
                        }

                        ledgePoint = floorHit.point;
                        return true;
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

        Debug.DrawRay(origin, direction * ledgeRayDistance, Color.red);
        return false;
    }

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
