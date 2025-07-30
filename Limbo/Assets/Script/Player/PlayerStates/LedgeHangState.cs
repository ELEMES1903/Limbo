using UnityEngine;

public class LedgeHangState : PlayerStateBase
{

    Vector3 wallNormal;

    [Header("LedgeHang Movement")]
    private Vector3 ledgePoint;
    private float horizontalOffset = 0.7f;
    private float verticalOffset = -1.0f;
    private float hangMoveSpeed = 4f;
    private float rotationSpeed = 5f;

    [Header("Ledge Camera")]
    public float maxYaw = 90f;
    public float maxPitch = 90f;

    [Header("Ledge Jump")]
    public float ledgeJumpDistance = 7f;
    public float ledgeJumpHeight = 3f;

    [Header("Ledge Drop")]
    private float backHangTimer = 0f;
    private const float backHangThreshold = 1f;
    private bool isHoldingBack = false;
    private float buffer = 10f;

    public LedgeHangState(PlayerStateManager manager, Vector3 ledgePoint, Vector3 wallNormal) : base(manager)
    {
        this.ledgePoint = ledgePoint;
        this.wallNormal = wallNormal;
    }

    public override void EnterState()
    {
        manager.inState = true;
        manager.rotatePlayerToCamera = false;

        // Reposition to ledge anf face towards wall
        Vector3 hangPosition = ledgePoint - manager.transform.forward * horizontalOffset + Vector3.up * verticalOffset;
        manager.rb.position = hangPosition;
        manager.transform.forward = -wallNormal;

        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;

        /*
        // ✅ Save current player rotation
        Quaternion originalPlayerRotation = manager.transform.rotation;

        // ✅ Reorient player to face wall
        Quaternion targetRotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
        manager.transform.rotation = targetRotation;

        // ✅ Match cameraHolder rotation to previously saved player rotation
        manager.cameraHolder.rotation = originalPlayerRotation;

        // ✅ Set pitch and yaw based on new cameraHolder orientation
        Vector3 camAngles = manager.cameraHolder.localEulerAngles;
        manager.pitch = (camAngles.x > 180) ? camAngles.x - 360 : camAngles.x;
        manager.yaw = (camAngles.y > 180) ? camAngles.y - 360 : camAngles.y;
        */
    }

    public override void UpdateState()
    {
        LedgeMovement();
        //LookLimited();
        manager.Look();
        LedgeJump();
        LedgeDrop();

        if (buffer > 0f)
        {
            manager.rb.linearVelocity = Vector3.zero;
            buffer--;
        }
        //Debug.Log("Velocity before zeroing: " + manager.rb.linearVelocity);
    }

    public override void FixedUpdateState()
    {

    }
    public void LedgeMovement()
    {
        if (manager.exitingLedgeHang)
            return;
            
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
        float input = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(input) > 0.1f)
        {
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);
            Vector3 moveDir = -wallRight * input;

            // ✅ Move with Rigidbody
            Vector3 targetPosition = manager.rb.position + moveDir * hangMoveSpeed * Time.deltaTime;
            manager.rb.MovePosition(targetPosition);

            // ✅ Rotate toward wall using Rigidbody
            Quaternion targetRotation = Quaternion.LookRotation(-wallNormal);
            Quaternion newRotation = Quaternion.Slerp(manager.rb.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            manager.rb.MoveRotation(newRotation);
        }
    }

    public void LookLimited()
    {
        float mouseX = Input.GetAxis("Mouse X") * manager.mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * manager.mouseSensitivity * Time.deltaTime;

        manager.yaw += mouseX;
        manager.pitch -= mouseY;

        manager.yaw = Mathf.Clamp(manager.yaw, -maxYaw, maxYaw);
        manager.pitch = Mathf.Clamp(manager.pitch, -maxPitch, maxPitch);

        manager.cameraHolder.localRotation = Quaternion.Euler(manager.pitch, manager.yaw, 0f);
    }

    public void LedgeJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Calculate jump direction
            Vector3 jumpDirection = wallNormal.normalized * ledgeJumpDistance + Vector3.up * ledgeJumpHeight;

            // Mark we're exiting ledge hang
            manager.exitingLedgeHang = true;

            // Exit ledge hang state
            manager.SwitchState(new AirState(manager));

            // Clear current velocity and apply the jump force
            //manager.rb.linearVelocity = Vector3.zero; // Optional, prevents stacking velocity
            manager.rb.AddForce(jumpDirection, ForceMode.VelocityChange);
        }
    }

    private void LedgeDrop()
    {
        if (Input.GetKey(KeyCode.S))
        {
            if (!isHoldingBack)
            {
                isHoldingBack = true;
                backHangTimer = 0f;
            }

            backHangTimer += Time.deltaTime;

            if (backHangTimer >= backHangThreshold)
            {
                manager.exitingLedgeHang = true;
                manager.SwitchState(new AirState(manager));
            }
        }
        else
        {
            // Reset if they let go early
            isHoldingBack = false;
            backHangTimer = 0f;
        }
    }

    public override void ExitState()
    {
        manager.inState = false;
        manager.rotatePlayerToCamera = true;
        /*
        // Save current cameraHolder rotation
        Quaternion savedCameraRotation = manager.cameraHolder.rotation;
        // Apply cameraHolder Y rotation to player using Rigidbody
        Quaternion targetRotation = Quaternion.Euler(0f, savedCameraRotation.eulerAngles.y, 0f);
        manager.rb.MoveRotation(targetRotation);
        // Realign cameraHolder to match player's new forward with current pitch
        manager.cameraHolder.localRotation = Quaternion.Euler(manager.pitch, 0f, 0f);
        */
        Debug.Log("After ExitState6: " + manager.transform.rotation.eulerAngles);
    }

}
