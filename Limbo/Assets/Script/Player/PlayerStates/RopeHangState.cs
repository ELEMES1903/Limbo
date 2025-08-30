using Unity.VisualScripting;
using UnityEngine;
using Dreamteck.Splines;
using UnityEngine.InputSystem;

public class RopeHangState : PlayerStateBase
{
    public float RopeHangCooldown = 1f;

    [Header("Climb")]
    public float climbSpeed = 1;
    private float climbInputValue;
    private float rotateInputValue;
    private Vector2 swayInputValue;

    [Header("Climb Limit Check")]
    private float climbCheckDistance = .5f;
    [Header("Climb")]
    Vector3 nodeVelocity;
    Vector3 nodeDirection;

    public RopeHangState(PlayerStateManager manager) : base(manager) { }
    public override void EnterState()
    {
        manager.playerInputActions.RopeHang.Enable();
        manager.playerInputActions.RopeHang.Drop.performed += Drop;
        manager.playerInputActions.RopeHang.Jump.performed += Jump;

        manager.inState = true;
        manager.rotatePlayerToCamera = false;
        manager.previousPlayerYaw = manager.transform.eulerAngles.y;
        manager.CalibrateCamera();

        manager.splineFollower.direction = GetLandingDirection(manager.splineFollower.transform.position, manager.splineFollower.transform.forward, manager.splineFollower);
        manager.splineFollower.follow = true;

        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    public override void UpdateState()
    {
        climbInputValue = manager.playerInputActions.RopeHang.Climb.ReadValue<float>();
        rotateInputValue = manager.playerInputActions.RopeHang.Rotate.ReadValue<float>();
        swayInputValue = manager.playerInputActions.RopeHang.Sway.ReadValue<Vector2>();


        manager.rope.ApplyPlayerInputToRope(manager.transform, swayInputValue, 0.15f, out Vector3 currentNodePos, out Vector3 prevNodePos);
        nodeVelocity = (currentNodePos - prevNodePos) / Time.fixedDeltaTime;
        nodeDirection = nodeVelocity.normalized;

        manager.Look();
        AlignPlayerToRope();
        HandleRopeRotation();
        Climb();
    }
    public override void FixedUpdateState() {}

    private void Climb()
    {
        Debug.Log(climbInputValue);
        if (Mathf.Abs(climbInputValue) > 0.1f)
        {
            //manager.splineFollower.follow = true;

            if (climbInputValue == 1)
            {
                if (!FloorCheck())
                { manager.splineFollower.followSpeed = climbSpeed; }
                else
                { manager.splineFollower.followSpeed = 0; }
            }
            else if (climbInputValue == -1)
            {
                if (!CeilingCheck())
                { manager.splineFollower.followSpeed = -climbSpeed; }
                else
                { manager.splineFollower.followSpeed = 0; }
            }
        }
        else
        {
            //no input detected = dont move
            //manager.splineFollower.follow = false;
            manager.splineFollower.followSpeed = 0;
        }
    }

    private bool CeilingCheck()
    {
        RaycastHit ceilingHit;
        if (Physics.Raycast(manager.ceilingCheck.position, manager.transform.up, out ceilingHit, climbCheckDistance, manager.ceilingLayer))
        {
            return true;
        }
        Debug.DrawRay(manager.ceilingCheck.position, manager.transform.up * climbCheckDistance, Color.red, 0.1f);
        return false;
    }

    private bool FloorCheck()
    {
        RaycastHit floorHit;
        if (Physics.Raycast(manager.groundCheck.position, -manager.transform.up, out floorHit, climbCheckDistance, manager.floorLayer))
        {
            return true;
        }
        Debug.DrawRay(manager.groundCheck.position, -manager.transform.up * climbCheckDistance, Color.blue, 0.1f);
        return false;
    }

    private void Drop(InputAction.CallbackContext context) { manager.SwitchState(new AirState(manager)); }
    private void Jump(InputAction.CallbackContext context)
    {
        // Base rope swing momentum
        //Vector3 ropeMomentum = nodeDirection * nodeVelocity.magnitude;

        // Optional: add upward boost so jumps feel intentional, not just "let go"
        Vector3 jumpBoost = manager.transform.up * manager.ropeJumpForce; // jumpForce is a tunable float

        float velocityBoost = 1.75f;

        // Combine swing velocity and jump impulse
        rb.linearVelocity = nodeVelocity * velocityBoost + jumpBoost;

        manager.SwitchState(new AirState(manager));
        manager.transform.up = Vector3.up;
    }

    private void AlignPlayerToRope()
    {
        // Evaluate spline at a given percent
        SplineSample sample = manager.splineFollower.Evaluate(manager.splineFollower.result.percent);
        // Desired up direction is spline's forward (tangent)
        Vector3 desiredUp = sample.forward;

        // To get a forward vector perpendicular to desiredUp, project current forward onto plane perpendicular to desiredUp
        Vector3 projectedForward = Vector3.ProjectOnPlane(manager.transform.forward, desiredUp);

        if (projectedForward.sqrMagnitude < 0.001f)
        {
            // If projected forward is almost zero vector (looking directly up/down), fallback to a default forward
            projectedForward = Vector3.Cross(desiredUp, Vector3.right);
        }

        manager.transform.rotation = Quaternion.LookRotation(projectedForward.normalized, -desiredUp);
    }

    Spline.Direction GetLandingDirection(Vector3 position, Vector3 forward, SplineFollower follower) 
    {
        SplineSample result = new();
        follower.Project(manager.ropeContactPoint, ref result);
        
        // Clamp to avoid snapping to absolute ends
        float clampedPercent = Mathf.Clamp01((float)result.percent);
        clampedPercent = Mathf.Clamp(clampedPercent, 0.01f, 0.99f);

        manager.splineFollower.SetPercent(clampedPercent);

        float dot = Vector3.Dot(result.forward, forward);
        return (Spline.Direction)Mathf.Sign(dot);
    }

    private void HandleRopeRotation()
    {
        if (Mathf.Abs(rotateInputValue) > 0.01f) // deadzone
        {
            // Rotation speed
            float rotationSpeed = 100f; // degrees per second, tweak as needed

            // Rotate the player around their up axis
            manager.transform.Rotate(manager.transform.up, rotateInputValue * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    public override void ExitState()
    {
        manager.ropeDetected = false;
        manager.rope.playerDetected = false;
        manager.inState = false;
        manager.splineFollower.follow = false;
        manager.StartTimer("RopeHangCooldown", RopeHangCooldown);

        manager.transform.up = Vector3.up;

        manager.playerInputActions.RopeHang.Disable();
    }
}
