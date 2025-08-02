using UnityEngine;
using System.Collections;

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

    [Header("Ledge Camera")]
    public float maxYaw = 90f;
    public float maxPitch = 90f;

    [Header("Ledge Jump")]
    public float ledgeJumpDistance = 7f;
    public float ledgeJumpHeight = 6f;

    [Header("Ledge Drop")]
    private float backHangTimer = 0f;
    private const float backHangThreshold = 1f;
    private bool isHoldingBack = false;

    [Header("Ledge Drop")]
    private float ledgeHangCooldown = 0.5f;

    public LedgeHangState(PlayerStateManager manager, Vector3 ledgePoint, Vector3 wallNormal) : base(manager)
    {
        this.ledgePoint = ledgePoint;
        this.wallNormal = wallNormal;
    }

    public override void EnterState()
    {
        manager.inState = true;
        manager.rotatePlayerToCamera = false;
        manager.previousPlayerYaw = manager.transform.eulerAngles.y;

       // Get the yaw (horizontal angle) of the player and camera
        float playerYaw = manager.transform.eulerAngles.y;
        float cameraYaw = manager.cameraHolder.eulerAngles.y;

        // Calculate the shortest signed angle between the player and camera yaw
        // This handles wraparound (e.g., 359° vs 1° becomes +2°, not -358°)
        // This value is stored to define the "initial" camera offset when entering ledgehang
        manager.initialYaw = Mathf.DeltaAngle(playerYaw, cameraYaw);

        //manager.StartCoroutine(SmoothToLedgeHang(ledgePoint, wallNormal));

        //Reposition to ledge anf face towards wall
        manager.transform.forward = -wallNormal;
        Vector3 hangPosition = ledgePoint - manager.transform.forward * horizontalOffset + Vector3.up * verticalOffset;
        manager.rb.position = hangPosition;

        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    /*
    private IEnumerator SmoothToLedgeHang(Vector3 ledgePoint, Vector3 wallNormal)
    {
        // Target position = slightly below the ledge and offset from the wall
        Vector3 targetPosition = ledgePoint - manager.transform.forward * horizontalOffset + Vector3.up * verticalOffset;
        Quaternion targetRotation = Quaternion.LookRotation(-wallNormal);

        Vector3 startPosition = manager.rb.position;
        Quaternion startRotation = manager.transform.rotation;

        float duration = 0.2f;
        float t = 0f;

        // Freeze player motion while transitioning
        manager.rb.isKinematic = true;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            manager.rb.position = Vector3.Lerp(startPosition, targetPosition, t);
            manager.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        // Final snap to exact position and rotation
        manager.rb.position = targetPosition;
        manager.transform.rotation = targetRotation;
        manager.rb.isKinematic = false;
    }
    */
    public override void UpdateState()
    {
        LedgeMovement();
        manager.Look();
        LedgeJump();
        LedgeDrop();
        manager.ResetPlayerVelocity();
    }

    public override void FixedUpdateState()
    {

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

    public void LedgeJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            // Calculate jump direction
            Vector3 jumpDirection = wallNormal.normalized * ledgeJumpDistance + Vector3.up * ledgeJumpHeight;

            // Exit ledge hang state
            manager.SwitchState(new AirState(manager));

            // Clear current velocity and apply the jump force
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
        manager.StartTimer("LedgeHangCooldown", ledgeHangCooldown);
    }

}
