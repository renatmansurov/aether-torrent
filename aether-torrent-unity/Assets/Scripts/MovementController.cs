using UnityEngine;

public class MovementController
{
    private readonly CharacterController characterController;
    private readonly Animator animator;
    private readonly float maxSpeed;
    private readonly float movementLerpSpeed;
    private readonly float turnSmoothTime;
    private readonly int chrSpeedID;

    private Vector2 inputMovement;
    private Vector3 direction;
    private float turnSmoothVelocity;

    public float VerticalVelocity { get; set; }

    public MovementController(CharacterController characterController, Animator animator, float maxSpeed, float movementLerpSpeed, float turnSmoothTime, int chrSpeedID)
    {
        this.characterController = characterController;
        this.animator = animator;
        this.maxSpeed = maxSpeed;
        this.movementLerpSpeed = movementLerpSpeed;
        this.turnSmoothTime = turnSmoothTime;
        this.chrSpeedID = chrSpeedID;
    }

    public void SetInputMovement(Vector2 input)
    {
        inputMovement = input;
    }

    public void ApplyMovement()
    {
        var desiredVelocity = new Vector3(inputMovement.x, 0, inputMovement.y) * maxSpeed;
        var currentVelocity = characterController.velocity;
        var maxSpeedChange = movementLerpSpeed * Time.deltaTime;

        direction.x = Mathf.MoveTowards(currentVelocity.x, desiredVelocity.x, maxSpeedChange);
        direction.z = Mathf.MoveTowards(currentVelocity.z, desiredVelocity.z, maxSpeedChange);
        direction.y = VerticalVelocity;

        characterController.Move(direction * Time.deltaTime);
    }

    public void UpdateAnimator()
    {
        var charVelocity = characterController.velocity;
        var localVelocity = characterController.transform.InverseTransformDirection(charVelocity);
        var speed = localVelocity.z;
        animator.SetFloat(chrSpeedID, speed);
    }

    public void ApplyRotation()
    {
        if (inputMovement.sqrMagnitude == 0)
            return;

        var targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        var angle = Mathf.SmoothDampAngle(characterController.transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        characterController.transform.rotation = Quaternion.Euler(0.0f, angle, 0.0f);
    }
}