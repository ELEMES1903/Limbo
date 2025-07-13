using UnityEngine;

public class WalkingState : PlayerStateBase
{
    public WalkingState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState() {}

    public override void UpdateState()
    {
        manager.Look();
        manager.HandleMovementInput();
        manager.ApplyGravityAndJump();

        manager.MovePlayer(manager.moveSpeed);
    }

    public override void ExitState() {}
}

