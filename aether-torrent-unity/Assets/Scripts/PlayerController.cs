// Assets/Scripts/PlayerController.cs

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
    private StateMachine playerStateMachine;
    private MovementController movementController;

    [Header("Layer Masks")] public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("Jump Settings")] public float maxJumpHeight = 5f;
    public float doubleJumpMult = 1f;
    public float maxJumpTime = .5f;
    public float gravity = -9.81f;

    [FormerlySerializedAs("fallGravityMult")]
    public float jumpFallGravityMult = 2f;

    public const float BaseGravity = -50;
    public float jumpGravity;
    public float initialJumpVelocity;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;
    public int maxJumps = 2;
    public float hoverGravity = -9.81f;

    [Header("Movement Settings")] public float maxSpeed;
    [SerializeField] private float movementLerpSpeed;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float fallTime;

    [Header("Dash Settings")] public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashThreshold = 1f;
    public float maxFallLimit = 2f;
    public AnimationCurve dashCurve;
    public float dashSpeed;

    // Dash internal state (remains as provided)
    public bool isDashing;
    private float dashTimer;
    private Vector3 dashDirection;
    public float dashCooldownTimer;
    public int dashCount;
    private float currentDashSpeed;

    public float jumpBufferCounter;
    public bool holdJump;

    // Internal state variables
    [HideInInspector] public float lastGroundedTime = -999f;

    public Vector2 inputMovement;
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
    public Vector3 currentDashTarget;
    public Vector3 currentDashStart;

    // Animator parameter hashes
    public static readonly int ChrSpeedID = Animator.StringToHash("chrSpeed");
    public static readonly int JumpID = Animator.StringToHash("jump");
    public static readonly int IsFallingID = Animator.StringToHash("isFalling");
    public static readonly int LandID = Animator.StringToHash("land");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerStateMachine = new StateMachine();
        playerStateMachine.Initialize(new MovementState(this, playerStateMachine));
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

        playerStateMachine.HandleInput();
        playerStateMachine.Update();

        // Always apply rotation even while dashing
        movementController.ApplyRotation();

        // Set vertical velocity regardless of state
        movementController.VerticalVelocity = verticalVelocity;

        // Only apply movement and update animator when not dashing
        //if (!isDashing)
        {
            movementController.ApplyMovement();
            movementController.UpdateAnimator();
        }

        if (!IsGrounded() || startFall || isFalling)
            CheckFall();

        MeasureHeight();
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 50,
            normal =
            {
                textColor = Color.red
            }
        };
        GUIStyle styleSmall = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            normal =
            {
                textColor = Color.black
            }
        };
        GUILayout.Label("State:" + playerStateMachine.CurrentState, style);
        GUILayout.Label("Vertical Velocity: " + verticalVelocity, styleSmall);
        GUILayout.Label("Gravity: " + gravity, styleSmall);
    }

    private void FixedUpdate()
    {
        playerStateMachine.FixedUpdate();
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
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawLine(currentDashStart, currentDashTarget);
        Gizmos.DrawCube(currentDashTarget, characterController.bounds.size);
        Gizmos.DrawCube(currentDashStart, characterController.bounds.size);
        var lastJumpHeightPos = new Vector3(characterController.transform.position.x, lastJumpHeight - characterController.height / 2, characterController.transform.position.z);
        Handles.DrawWireDisc(lastJumpHeightPos, Vector3.up, 1);
        var style = new GUIStyle
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
            playerStateMachine.ChangeState(new DashState(this, playerStateMachine));
        }
    }

    public void InputMovement(InputAction.CallbackContext context)
    {
        inputMovement = context.ReadValue<Vector2>();
        movementController.SetInputMovement(inputMovement);
    }

    #endregion
}