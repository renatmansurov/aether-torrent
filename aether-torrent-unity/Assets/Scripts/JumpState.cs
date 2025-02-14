using UnityEngine;

public class JumpState : CharacterState
{
    public static int JumpCount;
    private bool jumping;
    private float jumpGravity;

    public JumpState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        if (!Player.CheckSlope() || (!Player.IsGrounded() && JumpCount >= Player.maxJumps))
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }
        jumping = true;
        Player.gravity = Player.jumpGravity;
        Debug.Log(JumpCount);
        Player.verticalVelocity = JumpCount == 0 ? Player.initialJumpVelocity * 0.5f : Player.initialJumpVelocity * 0.5f * Player.doubleJumpMult;
        JumpCount++;

        // (Optional) Trigger jump animation.
        //Player.animator.SetTrigger(Player.JumpID);
    }

    public override void HandleInput()
    {
        if (Player.jumpBufferCounter > 0 && JumpCount < Player.maxJumps - 1)
        {
            Player.jumpBufferCounter = 0;
            StateMachine.ChangeState(new JumpState(Player, StateMachine));
            return;
        }

        if (Player.IsGrounded() && !jumping)
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
        }
    }

    public override void Update()
    {
        if (Player.verticalVelocity <= 0)
        {
            StateMachine.ChangeState(new FallingState(Player, StateMachine));
            return;
        }

        if (!Player.IsGrounded())
        {
            if (jumping && Player.holdJump)
            {
                Player.gravity = Player.jumpGravity;
            }
            else if (!Player.holdJump)
            {
                Player.gravity = Player.jumpGravity * Player.jumpFallGravityMult;
                jumping = false;
            }
        }
    }

    public override void FixedUpdate()
    {
        Player.ApplyGravity();
    }
}