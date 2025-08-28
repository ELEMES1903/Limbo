using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class LedgeHangState : PlayerStateBase
{
    [Header("Parameters")]
    private Vector3 wallNormal;
    private Vector3 ledgePoint;

    [Header("LedgeHang Movement")]

    private float horizontalOffset = 0.7f;
    private float verticalOffset = -1.0f;
    private float hangMoveSpeed = 4f;
    private float rotationSpeed = 5f;
    private float shimmyInputValue;

    [Header("Ledge Camera")]
    public float maxYaw = 90f;
    public float maxPitch = 90f;

    [Header("Ledge Jump")]
    public float ledgeJumpDistance = 7f;
    public float ledgeJumpHeight = 6f;
    private bool ledgeJumpInputDetected;

    [Header("Ledge Drop")]
    private float ledgeHangCooldown = 0.5f;

    public LedgeHangState(PlayerStateManager manager, Vector3 ledgePoint, Vector3 wallNormal) : base(manager)
    {
        this.ledgePoint = ledgePoint;
        this.wallNormal = wallNormal;
    }

    public override void EnterState()
    {
        manager.playerInputActions.LedgeHang.LedgeJump.performed += DetectLedgeJumpInput;
        manager.playerInputActions.LedgeHang.LedgeDrop.performed += LedgeDrop;
        manager.playerInputActions.LedgeHang.Enable();

        manager.inState = true;
        manager.rotatePlayerToCamera = false;
        manager.previousPlayerYaw = manager.transform.eulerAngles.y;

        CalibrateCamera();

        //Reposition to ledge anf face towards wall
        manager.transform.forward = -wallNormal;
        Vector3 hangPosition = ledgePoint - manager.transform.forward * horizontalOffset + Vector3.up * verticalOffset;
        manager.rb.position = hangPosition;

        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }

    public override void UpdateState()
    {
        shimmyInputValue = manager.playerInputActions.LedgeHang.Shimmy.ReadValue<float>();

        manager.Look();
        manager.ResetPlayerVelocity();
    }

    public override void FixedUpdateState()
    {
        LedgeMovement();
        LedgeJump();
    }

    private void CalibrateCamera()
    {
        // Get the yaw (horizontal angle) of the player and camera
        float playerYaw = manager.transform.eulerAngles.y;
        float cameraYaw = manager.cameraHolder.eulerAngles.y;

        // Calculate the shortest signed angle between the player and camera yaw
        // This handles wraparound (e.g., 359° vs 1° becomes +2°, not -358°)
        // This value is stored to define the "initial" camera offset when entering ledgehang
        manager.initialYaw = Mathf.DeltaAngle(playerYaw, cameraYaw);
    }
    public void LedgeMovement()
    {
        // Raycasting to find wall for alignment and movement direction
        Vector3 origin = manager.ledgeRaycastOrigin.position;
        Vector3 forward = manager.ledgeRaycastOrigin.forward;

        float angleOffset = 25f;
        float rayDistance = manager.ledgeRayDistance;

        // Default: no wall
        bool gotWall = false;
        RaycastHit wallHit;

        // Main center ray
        if (Physics.Raycast(origin, forward, out wallHit, rayDistance, manager.wallLayer))
        {
            gotWall = true;
        }
        else
        {
            // Try left ray
            Vector3 leftDir = Quaternion.AngleAxis(-angleOffset, Vector3.up) * forward;
            Vector3 rightDir = Quaternion.AngleAxis(angleOffset, Vector3.up) * forward;
            if (Physics.Raycast(origin, leftDir, out wallHit, rayDistance, manager.wallLayer))
            {
                gotWall = true;
                forward = leftDir;
            }
            else if (Physics.Raycast(origin, rightDir, out wallHit, rayDistance, manager.wallLayer)) // Try right ray
            {
                gotWall = true;
                forward = rightDir;
            }
            else
            {
                manager.SwitchState(new AirState(manager)); // failsafe
            }
        }

        // Visual Debugging
        Debug.DrawRay(origin, forward * rayDistance, Color.red, 0.1f); // active ray
        Debug.DrawRay(origin, Quaternion.AngleAxis(-angleOffset, Vector3.up) * manager.ledgeRaycastOrigin.forward * rayDistance, Color.magenta, 0.1f); // left
        Debug.DrawRay(origin, Quaternion.AngleAxis(angleOffset, Vector3.up) * manager.ledgeRaycastOrigin.forward * rayDistance, Color.cyan, 0.1f); // right

        if (!gotWall) return;

        wallNormal = wallHit.normal;
        Debug.Log(shimmyInputValue);
        if (Mathf.Abs(shimmyInputValue) > 0.1f)
        {
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);
            Vector3 moveDir = -wallRight * shimmyInputValue;

            // ✅ Move with Rigidbody
            Vector3 targetPosition = manager.rb.position + moveDir * hangMoveSpeed * Time.deltaTime;
            manager.rb.MovePosition(targetPosition);

            // ✅ Rotate toward wall using Rigidbody
            Quaternion targetRotation = Quaternion.LookRotation(-wallNormal);
            Quaternion newRotation = Quaternion.Slerp(manager.rb.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            manager.rb.MoveRotation(newRotation);
        }
    }
    public void DetectLedgeJumpInput(InputAction.CallbackContext context) { ledgeJumpInputDetected = true; }
    public void LedgeJump()
    {
        if (ledgeJumpInputDetected)
        {
            // Calculate jump direction
            Vector3 jumpDirection = wallNormal.normalized * ledgeJumpDistance + Vector3.up * ledgeJumpHeight;

            // Exit ledge hang state
            manager.SwitchState(new AirState(manager));

            // Clear current velocity and apply the jump force
            manager.rb.AddForce(jumpDirection, ForceMode.VelocityChange);

            ledgeJumpInputDetected = false;
        }
    }
    private void LedgeDrop(InputAction.CallbackContext context) { manager.SwitchState(new AirState(manager)); }

    public override void ExitState()
    {
        manager.inState = false;
        manager.rotatePlayerToCamera = true;
        manager.StartTimer("LedgeHangCooldown", ledgeHangCooldown);

        manager.playerInputActions.LedgeHang.LedgeJump.performed -= DetectLedgeJumpInput;
        manager.playerInputActions.LedgeHang.LedgeDrop.performed -= LedgeDrop;
        manager.playerInputActions.LedgeHang.Disable();
    }
}
