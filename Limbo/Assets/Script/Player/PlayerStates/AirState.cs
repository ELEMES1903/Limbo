using UnityEngine;

public class AirState : PlayerStateBase
{
    public AirState(PlayerStateManager manager) : base(manager) {}

    public override void EnterState()
    { 
        manager.playerInputActions.InAir.Enable();
    }

    public override void UpdateState()
    {
        manager.Look();
    }
    public override void FixedUpdateState()
    {
        manager.CustomGravity();
    }

    public override void ExitState()
    { 
        manager.playerInputActions.InAir.Disable();        
    }
}
