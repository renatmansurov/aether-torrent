using UnityEngine;
using UnityEngine.Serialization;

public class PlayerJumpController : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController; // Ensure this is assigned (or use GetComponent in Awake)

    [Header("Jump Settings")]
    [Tooltip("Maximum jump height when jump is fully held.")]
    public float maxJumpHeight = 5f;
    [Tooltip("Low jump height when jump is tapped.")]
    public float lowJumpHeight = 2f;
    [Tooltip("Gravity (should be a negative value).")]
    public float gravity = -9.81f;
    [Tooltip("Time allowed after leaving the ground to still jump.")]
    public float coyoteTime = 0.2f;
    [Tooltip("Time window to buffer a jump input before landing.")]
    public float jumpBufferTime = 0.2f;
    [Tooltip("Total jumps allowed (2 for double jump).")]
    public int maxJumps = 2;

    // Internal state
    private float verticalVelocity = 0f;
    private float lastGroundedTime = -999f;
    private float jumpBufferCounter = 0f;
    private int jumpCount = 0;

    void Update()
    {
        // Check if grounded and update timers
        if (characterController.isGrounded)
        {
            // Reset vertical velocity when on the ground
            if (verticalVelocity < 0)
                verticalVelocity = 0f;

            lastGroundedTime = Time.time;
            jumpCount = 0;
        }

        // Handle jump input buffering.
        // On space key down, start the buffer timer.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        // Determine if the player is allowed to jump.
        // Allowed if grounded, within coyote time, or if a double jump is available.
        bool canJump = characterController.isGrounded ||
                       (Time.time - lastGroundedTime <= coyoteTime) ||
                       (jumpCount < maxJumps);

        // If jump is buffered and conditions are met, perform a jump.
        if (jumpBufferCounter > 0f && canJump)
        {
            Jump();
            jumpBufferCounter = 0f; // Reset the buffer after jumping
        }

        // Implement variable jump height.
        // If the jump button is released while moving upward,
        // clamp the vertical velocity to a value for a lower jump.
        if (Input.GetKeyUp(KeyCode.Space) && verticalVelocity > 0)
        {
            float lowJumpVelocity = Mathf.Sqrt(2f * -gravity * lowJumpHeight);
            if (verticalVelocity > lowJumpVelocity)
            {
                verticalVelocity = lowJumpVelocity;
            }
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // Apply vertical movement using the CharacterController.
        Vector3 move = new Vector3(0, verticalVelocity, 0);
        characterController.Move(move * Time.deltaTime);
    }

    void Jump()
    {
        // Increment jump count: if on ground (or within coyote time), jumpCount is reset; if in air, this counts as a double jump.
        jumpCount++;

        // Calculate the required initial jump velocity to reach the max jump height.
        // Formula: v = sqrt(2 * |gravity| * jumpHeight)
        verticalVelocity = Mathf.Sqrt(2f * -gravity * maxJumpHeight);
    }
}