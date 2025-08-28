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
    [Header("Climb Limit Check")]
    
    private float climbCheckDistance = .5f;
    
    public RopeHangState(PlayerStateManager manager) : base(manager) { }
    public override void EnterState()
    {
        manager.playerInputActions.RopeHang.Enable();
        manager.playerInputActions.RopeHang.RopeDrop.performed += RopeDrop;

        manager.inState = true;
        manager.rotatePlayerToCamera = false;

        manager.splineFollower.direction = GetLandingDirection(manager.splineFollower.transform.position, manager.splineFollower.transform.forward, manager.splineFollower);
    
        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    public override void UpdateState()
    {
        climbInputValue = manager.playerInputActions.RopeHang.Climb.ReadValue<float>();

        manager.Look();
        AlignPlayerToRope();
        Climb();
    }
    public override void FixedUpdateState() {}

    private void Climb()
    {
        Debug.Log(climbInputValue);
        if (Mathf.Abs(climbInputValue) > 0.1f)
        {
            manager.splineFollower.follow = true;

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
            manager.splineFollower.follow = false;
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

    private void RopeDrop(InputAction.CallbackContext context) { manager.SwitchState(new AirState(manager)); }

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

    public override void ExitState()
    {
        manager.ropeDetected = false;
        manager.rope.playerDetected = false;
        manager.inState = false;
        manager.StartTimer("RopeHangCooldown", RopeHangCooldown);

        manager.playerInputActions.RopeHang.Disable();
    }
}
