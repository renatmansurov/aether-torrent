using UnityEngine;

public class StateMachine
{
    public CharacterState CurrentState { get; private set; }

    public void Initialize(CharacterState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(CharacterState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void HandleInput()
    {
        CurrentState.HandleInput();
    }

    public void Update()
    {
        CurrentState.Update();
    }

    public void FixedUpdate()
    {
        CurrentState.FixedUpdate();
    }
}