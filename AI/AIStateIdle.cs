using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateIdle : AIState
{
    [SerializeField] private float _idleTime = 5f;

    private float _timer = 0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Idle;
    }

    public override void OnEnter()
    {
        _timer = 0f;
        _stateMachine.Animator.SetLayerWeight(_stateMachine.Animator.GetLayerIndex("Alert Layer"), 0);
        AISoundManager.SetPlayingSoundType(ESoundType.Roaming);
    }

    public override AIStateType OnUpdate()
    {
        if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type != AITargetType.NavigationPoint && _stateMachine.CurrentTarget.Type != AITargetType.None)
        {
            // We have a target that's not a navigation point, we go into alert mode
            _stateMachine.Animator.SetLayerWeight(_stateMachine.Animator.GetLayerIndex("Alert Layer"), 1);
            return AIStateType.Alert;
        }

        HandleAnimator();

        _timer += Time.deltaTime;
        if (_timer >= _idleTime)
        {
            return AIStateType.Patrol;
        }

        return AIStateType.Idle;
    }

    public override void OnExit()
    {
    }

    public override void HandleAnimator()
    {
        //_stateMachine.Animator.SetBool("IsWalking", _stateMachine.Agent.desiredVelocity.magnitude > 0);
    }
}
