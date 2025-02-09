using UnityEngine;

public class JumpState : CharacterState
{
    // Static jumpCount tracks the total number of jumps performed in the air.
    public static int JumpCount;
    private float jumpSustainTimer;
    private bool jumping;

    public JumpState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        // Check if a jump is allowed:
        // - If the slope is too steep, or
        // - If in air and we've already reached max jumps, then transition back.
        if (!Player.CheckSlope() || (!Player.IsGrounded() && JumpCount >= Player.maxJumps))
        {
            StateMachine.ChangeState(new MovementState(Player, StateMachine));
            return;
        }

        // Increment jump count and calculate the jump impulse.
        JumpCount++;
        var jumpHeight = JumpCount == 1 ? Player.maxJumpHeight : Player.doubleJumpHeight;
        var targetImpulse = Mathf.Sqrt(2f * -Player.gravity * jumpHeight);

        // Apply the jump impulse (if already moving upward, choose the higher velocity).
        Player.verticalVelocity = Player.verticalVelocity > 0 ? Mathf.Max(Player.verticalVelocity, targetImpulse) : targetImpulse;

        // Set jump-related flags.
        jumping = true;
        jumpSustainTimer = 0f;

        // (Optional) Trigger jump animation.
        // Player.animator.SetTrigger(Player.JumpID);
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
            var currentSustainForce = JumpCount == 1 ? Player.jumpSustainForce : Player.doubleJumpSustainForce;
            var currentMaxSustainTime = JumpCount == 1 ? Player.maxJumpSustainTime : Player.maxDoubleJumpSustainTime;
            if (jumping && Player.holdJump && jumpSustainTimer < currentMaxSustainTime)
            {
                Player.verticalVelocity += currentSustainForce * Time.fixedDeltaTime;
                jumpSustainTimer += Time.fixedDeltaTime;
            }
            else if (!Player.holdJump)
            {
                var lowJumpVelocity = Mathf.Sqrt(2f * -Player.gravity * Player.lowJumpHeight);
                if (Player.verticalVelocity > lowJumpVelocity)
                {
                    Player.verticalVelocity = lowJumpVelocity;
                }

                jumping = false;
            }
        }
    }

    public override void FixedUpdate()
    {
        Player.ApplyGravity();
        Player.ApplyMovement();
    }
}