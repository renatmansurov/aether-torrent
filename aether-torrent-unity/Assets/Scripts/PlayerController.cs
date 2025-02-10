// Assets/Scripts/PlayerController.cs

using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")] public CharacterController characterController;
    public Animator animator;
    private StateMachine stateMachine;
    private MovementController movementController;

    [Header("Jump Settings")]
    public float maxJumpHeight = 5f;
    public float doubleJumpMult = 1f;
    public float maxJumpTime = .5f;
    public float gravity = -9.81f;
    public float fallGravityMult = 2f;
    public const float BaseGravity = -9.81f;
    public float jumpGravity;
    public float initialJumpVelocity;


    [Tooltip("Time allowed after leaving the ground to still jump (coyote time).")]
    public float coyoteTime = 0.2f;

    [Tooltip("Time window to buffer a jump input before landing.")]
    public float jumpBufferTime = 0.2f;

    [Tooltip("Total jumps allowed (2 for double jump).")] [SerializeField]
    public int maxJumps = 2;

    [Header("Movement Settings")] public float maxSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
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
    private float currentHeight;
    private float lastHeight;
    public float lastJumpHeight;

    // Animator parameter hashes
    private static readonly int ChrSpeedID = Animator.StringToHash("chrSpeed");
    private static readonly int JumpID = Animator.StringToHash("jump");
    private static readonly int IsFallingID = Animator.StringToHash("isFalling");
    private static readonly int LandID = Animator.StringToHash("land");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        stateMachine = new StateMachine();
        stateMachine.Initialize(new MovementState(this, stateMachine));
        movementController = new MovementController(characterController, animator, maxSpeed, movementLerpSpeed, turnSmoothTime, ChrSpeedID);
        JumpVariablesSetup();
    }

    private void JumpVariablesSetup()
    {
        var timeToApex = maxJumpTime / 2f;
        jumpGravity = -2f * maxJumpHeight / Mathf.Pow(timeToApex, 2f);
        initialJumpVelocity = 2 * maxJumpHeight / timeToApex;
    }

    private void OnValidate()
    {
        JumpVariablesSetup();
    }

    private void Update()
    {
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        stateMachine.HandleInput();
        stateMachine.Update();

        movementController.ApplyRotation();
        movementController.VerticalVelocity = verticalVelocity;
        movementController.ApplyMovement();
        movementController.UpdateAnimator();

        if (!IsGrounded() || startFall || isFalling)
            CheckFall();

        MeasureHeight();
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            var dashMove = dashDirection * currentDashSpeed;
            //dashMove.y = verticalVelocity;
            characterController.Move(dashMove * Time.fixedDeltaTime);

            if ((dashTimer -= Time.fixedDeltaTime) <= 0f)
                isDashing = false;
        }
        else
        {
            stateMachine.FixedUpdate();
        }
    }

    #region Helper Methods

    public bool IsGrounded() => characterController.isGrounded;

    public bool CanJump()
    {
        return IsGrounded() ||
               Time.time - lastGroundedTime <= coyoteTime ||
               JumpState.JumpCount < maxJumps;
    }

    public void ApplyGravity()
    {
        if (IsGrounded() && verticalVelocity <= 0f)
        {
            // Slight downward velocity to keep the character grounded.
            verticalVelocity = verticalVelocity < 0f ? -0.1f : verticalVelocity;
            lastGroundedTime = Time.time;
            JumpState.JumpCount = 0;
            dashCount = 0;
        }
        else
        {
            var previousYVelocity = verticalVelocity;
            var newYVelocity = verticalVelocity + gravity * Time.deltaTime;
            var nextYVelocity = (previousYVelocity + newYVelocity) * 0.5f;
            verticalVelocity = nextYVelocity;
        }
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

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        Vector3 lastJumpHeightPos = new Vector3(characterController.transform.position.x, lastJumpHeight - characterController.height / 2, characterController.transform.position.z);
        Handles.DrawWireDisc(lastJumpHeightPos, Vector3.up, 1);
        GUIStyle style = new GUIStyle
        {
            normal =
            {
                textColor = Color.red
            },
            fontSize = 20
        };
        Handles.Label(lastJumpHeightPos, "Last Jump:" + (lastJumpHeight - characterController.height / 2).ToString(CultureInfo.InvariantCulture), style);
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
        movementController.SetInputMovement(inputMovement);
    }

    #endregion
}