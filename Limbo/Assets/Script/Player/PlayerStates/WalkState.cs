using UnityEngine;

public class WalkingState : PlayerStateBase
{
    public WalkingState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState() {}

    public override void UpdateState()
    {

        manager.HandleMovementInput();
       // manager.CustomGravity();

        manager.MovePlayer();
        manager.Look();
    }
    public override void FixedUpdateState()
    {
        
    }
    public override void ExitState() {}
}

