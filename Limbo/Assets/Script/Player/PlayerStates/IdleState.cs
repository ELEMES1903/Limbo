using UnityEngine;

public class IdleState : PlayerStateBase
{
    public IdleState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState() {}

    public override void UpdateState()
    {
        manager.Look();
        manager.HandleMovementInput();
        manager.ApplyGravityAndJump();

        manager.MovePlayer(0f);
    }

    public override void ExitState() {}
}
