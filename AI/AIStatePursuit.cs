using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStatePursuit : AIState
{
    [SerializeField] private float _followRotationSpeed = 10f;
    [SerializeField] private float _newDestinationDelay = 1f;

    private float _newDestinationTimer = 0f;

    public override AIStateType GetStateType()
    {
        return AIStateType.Pursuit;
    }

    public override void OnEnter()
    {
        _stateMachine.Animator.SetLayerWeight(_stateMachine.Animator.GetLayerIndex("Alert Layer"), 1);
        _newDestinationTimer = _newDestinationDelay;
        AISoundManager.SetPlayingSoundType(ESoundType.Running);
    }

    public override AIStateType OnUpdate()
    {
        // Sometimes we are pusuing something but the target is null. We would like to still go to the last position of the target. 
        if (_stateMachine.CurrentTarget == null)
        {
            if (_stateMachine.Agent.hasPath)
            {
                if (!_stateMachine.Agent.pathPending &&
                    (_stateMachine.Agent.remainingDistance <= _stateMachine.Agent.stoppingDistance
                    || _stateMachine.Agent.isPathStale))
                    return AIStateType.Alert;
            } else
            {
                return AIStateType.Alert;
            }
        }

        // Periodically setting a new destination
        _newDestinationTimer += Time.deltaTime;
        if (_newDestinationTimer >= _newDestinationDelay)
        {
            _newDestinationTimer = 0f;
            if (_stateMachine.CurrentTarget != null)
                _stateMachine.Agent.SetDestination(_stateMachine.CurrentTarget.TargetTransform.position);
        }

        // If we can't reach the target, we go into hurdle mode
        if (_stateMachine.Agent.isPathStale)
        {
            return AIStateType.Hurdle;
        }

        if (_stateMachine.Agent.remainingDistance <= _stateMachine.Agent.stoppingDistance
            && !_stateMachine.Agent.isPathStale)
        {
            if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type == AITargetType.Player
                && Vector3.Distance(transform.position, _stateMachine.CurrentTarget.TargetTransform.position ) <= _stateMachine.Agent.stoppingDistance)
            {
                // We go into attack mode
                return AIStateType.Attack;
            }
            if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type == AITargetType.Food)
            {
                // We go into feeding mode
                _stateMachine.ResetTarget();
                return AIStateType.Feeding;
            }
            if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type == AITargetType.Sound)
            {
                // We reset the target and go into sound mode if it's a sound
                _stateMachine.ResetTarget();
                return AIStateType.Alert;
            }
        }

        // Keeping the objective in sight
        if (_stateMachine.Agent.desiredVelocity != Vector3.zero)
        {
            Quaternion nextRotation = Quaternion.LookRotation(_stateMachine.Agent.desiredVelocity);
            transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, _followRotationSpeed * Time.deltaTime);
        }

        HandleAnimator();

        return AIStateType.Pursuit;
    }

    public override void OnExit()
    {
        
    }

    public override void HandleAnimator()
    {
        _stateMachine.Animator.SetInteger("Speed", _stateMachine.Agent.desiredVelocity.magnitude > 0 ? 1 : 0);
    }


}
