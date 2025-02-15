public abstract class CharacterState
{
    protected readonly PlayerController Player;
    protected readonly StateMachine StateMachine;

    protected CharacterState(PlayerController player, StateMachine stateMachine)
    {
        this.Player = player;
        this.StateMachine = stateMachine;
    }

    /// <summary>
    /// Called once when the state is entered.
    /// </summary>
    public virtual void Enter()
    {
    }

    /// <summary>
    /// Called once when the state is exited.
    /// </summary>
    public virtual void Exit()
    {
    }

    /// <summary>
    /// Handle input events.
    /// </summary>
    public virtual void HandleInput()
    {
    }

    /// <summary>
    /// Update called from MonoBehaviour.Update().
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// FixedUpdate called from MonoBehaviour.FixedUpdate().
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// Draw Debug Gizmo
    /// </summary>
    public virtual void DrawGizmo()
    {
    }
}