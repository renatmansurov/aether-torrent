using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")] public CharacterController characterController;
    public Animator animator;
    private StateMachine stateMachine;

    [Header("Jump Settings")] [Tooltip("Maximum jump height when jump is fully held.")]
    public float maxJumpHeight = 5f;

    [Tooltip("Low jump height when jump is tapped.")]
    public float lowJumpHeight = 2f;

    [Tooltip("Maximum jump height for a double jump.")]
    public float doubleJumpHeight = 3f;

    [Tooltip("Gravity (should be a negative value).")]
    public float gravity = -9.81f;

    [Tooltip("Time allowed after leaving the ground to still jump (coyote time).")]
    public float coyoteTime = 0.2f;

    [Tooltip("Time window to buffer a jump input before landing.")]
    public float jumpBufferTime = 0.2f;

    [Tooltip("Total jumps allowed (2 for double jump).")] [SerializeField]
    public int maxJumps = 2;

    [Header("Jump Sustain Settings")] [Tooltip("Additional upward force applied while the jump button is held on the first jump.")] [SerializeField]
    public float jumpSustainForce = 20f;

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the first jump.")] [SerializeField]
    public float maxJumpSustainTime = 0.2f;

    [Tooltip("Additional upward force applied while the jump button is held on the double jump.")] [SerializeField]
    public float doubleJumpSustainForce = 10f;

    [Tooltip("Maximum duration (in seconds) for the jump sustain phase on the double jump.")] [SerializeField]
    public float maxDoubleJumpSustainTime = 0.1f;

    [Header("Movement Settings")] public float maxSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float gravityMult = 3f;
    [SerializeField] private float jumpGravityMult = 3f;
    [SerializeField] private float fallTime;

    [Header("Dash Settings")] public float dashDistance = 5f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;

    // Dash internal state (remains as provided)
    private bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;
    private float dashCooldownTimer;
    private int dashCount;
    private float currentDashSpeed;

    public float jumpBufferCounter;
    public bool holdJump;


    // Internal state variables
    [HideInInspector] public float lastGroundedTime = -999f;



    private Vector2 inputMovement;
    public Vector2 inputLook;
    public Vector3 direction;
    public float verticalVelocity;

    private bool isFalling;
    private bool startFall;
    private float startFallTime;
    private float turnSmoothVelocity;

    // Debug Data
    float currentHeight;
    float lastHeight;
    public float lastJumpHeight;

    // Animator parameter hashes
    private static readonly int ChrSpeedID = Animator.StringToHash("chrSpeed");
    private static readonly int JumpID = Animator.StringToHash("jump");
    private static readonly int IsFallingID = Animator.StringToHash("isFalling");
    private static readonly int LandID = Animator.StringToHash("land");


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Initialize the state machine with the MovementState as the starting state.
        stateMachine = new StateMachine();
        stateMachine.Initialize(new MovementState(this, stateMachine));
    }

    private void Update()
    {
        // Decrement jump buffer timer if active.
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        // Decrement dash cooldown timer if active.
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // Let the state machine handle input and update logic.
        stateMachine.HandleInput();
        stateMachine.Update();

        ApplyRotation();
        UpdateAnimator();

        if (!IsGrounded() || startFall || isFalling)
            CheckFall();

        MeasureHeight();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            ApplyStandardGravity();
            var dashMove = dashDirection * currentDashSpeed;
            dashMove.y = verticalVelocity;
            characterController.Move(dashMove * Time.fixedDeltaTime);

            if ((dashTimer -= Time.fixedDeltaTime) <= 0f)
                isDashing = false;
        }
        else
        {
            // Delegate physics updates to the state machine.
            stateMachine.FixedUpdate();
        }
    }

    #region Helper Methods

    public bool IsGrounded() => characterController.isGrounded;

    /// <summary>
    /// Checks if the player is allowed to jump.
    /// </summary>
    public bool CanJump()
    {
        return IsGrounded() ||
               (Time.time - lastGroundedTime <= coyoteTime) ||
               (JumpState.jumpCount < maxJumps);
    }

    public void ApplyGravity()
    {
        if (IsGrounded() && verticalVelocity <= 0)
        {
            verticalVelocity = (verticalVelocity < 0) ? -1f : verticalVelocity;
            lastGroundedTime = Time.time;
            // Reset the jump count when landing:
            JumpState.jumpCount = 0;
            dashCount = 0;
            // (Optional) You might also want to clear any jump-related flags here if needed.
        }
        else
        {
            ApplyStandardGravity();
        }
        direction.y = verticalVelocity;
    }



    private void ApplyStandardGravity()
    {
        var currentGravityMult = verticalVelocity < 0 ? jumpGravityMult + gravityMult : gravityMult;
        verticalVelocity += gravity * currentGravityMult * Time.fixedDeltaTime;
    }

    public bool CheckSlope()
    {
        if (Physics.Raycast(new Ray(transform.position, Vector3.down), out var hitInfo, 0.2f))
        {
            Debug.Log("Slope normal: " + hitInfo.normal);
        }

        return true;
    }

    private void CheckFall()
    {
        if (IsGrounded())
        {
            startFall = isFalling = false;
            animator.SetBool(IsFallingID, false);
            animator.SetTrigger(LandID);
        }
        else if (!isFalling)
        {
            if (!startFall)
            {
                startFall = true;
                startFallTime = Time.time;
            }
            else if (Time.time >= startFallTime + fallTime)
            {
                isFalling = true;
                animator.SetBool(IsFallingID, true);
            }
        }
    }

    public void ApplyMovement()
    {
        var desiredVelocity = new Vector3(inputMovement.x, 0, inputMovement.y) * maxSpeed;
        var currentVelocity = characterController.velocity;
        var maxSpeedChange = movementLerpSpeed * Time.deltaTime;

        direction.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
        direction.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);

        characterController.Move(direction * Time.deltaTime);
    }

    public void UpdateAnimator()
    {
        var charVelocity = characterController.velocity;
        var localVelocity = transform.InverseTransformDirection(charVelocity);
        var speed = localVelocity.z;
        animator.SetFloat(ChrSpeedID, speed);
    }

    private void ApplyRotation()
    {
        if (inputMovement.sqrMagnitude == 0)
            return;

        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }

    private void MeasureHeight()
    {
        currentHeight = characterController.transform.position.y;
        if (currentHeight > lastHeight)
            lastJumpHeight = currentHeight;

        lastHeight = currentHeight;
    }

    #endregion

    #region Input Callbacks

    public void JumpPressed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpBufferCounter = jumpBufferTime;
            holdJump = true;
        }
        else if (context.canceled)
        {
            holdJump = false;
        }
    }

    public void DashPressed(InputAction.CallbackContext context)
    {
        if (context.started && dashCooldownTimer <= 0f && dashCount < 1)
        {
            dashDirection = inputMovement.sqrMagnitude > 0.1f
                ? new Vector3(inputMovement.x, 0f, inputMovement.y).normalized
                : transform.forward;

            isDashing = true;
            dashTimer = dashTime;
            currentDashSpeed = dashDistance / dashTime;
            dashCount++;
            dashCooldownTimer = dashCooldown;
        }
    }

    public void InputMovement(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
    }

    #endregion
}