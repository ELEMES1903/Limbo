using UnityEngine;

public abstract class PlayerStateBase
{
    protected PlayerStateManager manager;
    protected Rigidbody rb;
    protected Transform cameraHolder;

    public PlayerStateBase(PlayerStateManager manager)
    {
        this.manager = manager;
        this.rb = manager.rb;
        this.cameraHolder = manager.cameraHolder;
    }

    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void FixedUpdateState();
    public abstract void ExitState();
}
