public class MovementState : CharacterState
{
    public MovementState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        // Optionally trigger movement animation here.
        Player.animator.SetFloat("chrSpeed", 0f);
    }

    public override void HandleInput()
    {
        // If a jump input is buffered and the player is allowed to jump, transition to JumpState.
        if (Player.jumpBufferCounter > 0f && Player.CanJump())
        {
            StateMachine.ChangeState(new JumpState(Player, StateMachine));
        }
    }

    public override void Update()
    {
        // Handle regular movement and update animations.
        Player.ApplyMovement();
        Player.UpdateAnimator();
    }

    public override void FixedUpdate()
    {
        // Apply gravity and other physics.
        Player.ApplyGravity();
    }
}