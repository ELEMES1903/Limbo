using Unity.VisualScripting;
using UnityEngine;
using Dreamteck.Splines;
public class RopeHangState : PlayerStateBase
{
    public float RopeHangCooldown = 0.5f;
    
    public RopeHangState(PlayerStateManager manager) : base(manager){ }
    public override void EnterState()
    {
        manager.playerInputActions.RopeHang.Enable();

        manager.inState = true;
        manager.rotatePlayerToCamera = false;

        manager.splineFollower.direction = GetLandingDirection(manager.splineFollower.transform.position, manager.splineFollower.transform.forward, manager.splineFollower);
    
        // reset velocity
        manager.rb.linearVelocity = Vector3.zero;
        manager.rb.angularVelocity = Vector3.zero;
    }
    public override void UpdateState()
    {
        manager.Look();
        AlignPlayerToRope();
        RopeMovement();
    }
    public override void FixedUpdateState() {}

    private void RopeMovement()
    {
        
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
        follower.Project(position, ref result);
        float dot = Vector3.Dot(result.forward, forward);
        manager.splineFollower.SetPercent(result.percent);
        return (Spline.Direction)Mathf.Sign(dot);
    }

    public override void ExitState()
    {
        manager.ropeDetected = false;
        manager.StartTimer("LedgeHangCooldown", RopeHangCooldown);

        manager.playerInputActions.RopeHang.Disable();
    }
}
