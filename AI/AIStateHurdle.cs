using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateHurdle : AIState
{
    [SerializeField] private float _searchPathDelay = .5f;

    private float _searchTimer = 0f;
    private float _agentInitialSpeed = 0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Hurdle;
    }

    public override void OnEnter()
    {
        if (!_stateMachine.IsCrawling)
            _stateMachine.Animator.SetBool("IsHurdling", true);
        _searchTimer = _searchPathDelay;

        _agentInitialSpeed = _stateMachine.Agent.speed;
        _stateMachine.Agent.speed = 0f;
    }

    public override AIStateType OnUpdate()
    {
        if (_stateMachine.CurrentTarget == null)
        {
            return AIStateType.Idle;
        }

        _searchTimer += Time.deltaTime;
        if (_searchTimer >= _searchPathDelay)
        {
            _searchTimer = 0f;
            _stateMachine.Agent.SetDestination(_stateMachine.CurrentTarget.TargetTransform.position);
        }

        if (!_stateMachine.Agent.isPathStale)
        {
            return AIStateType.Pursuit;
        }

        // Keeping the objective in sight
        if (_stateMachine.Agent.desiredVelocity != Vector3.zero)
        {
            Quaternion toTargetRotation = Quaternion.LookRotation(_stateMachine.Agent.desiredVelocity);
            Quaternion nextRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, toTargetRotation.eulerAngles.y, transform.rotation.z);
            transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, 10f * Time.deltaTime);
        }

        HandleAnimator();

        return AIStateType.Hurdle;
    }

    public override void OnExit()
    {
        _stateMachine.Animator.SetBool("IsHurdling", false);
        _stateMachine.Agent.speed = _agentInitialSpeed;
    }

    public override void HandleAnimator()
    {
        if (_stateMachine.IsCrawling)
        {
            _stateMachine.Animator.SetBool("IsHurdling", false);
        }
    }

}
