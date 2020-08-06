using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStatePatrol : AIState
{
    [SerializeField] private AINavigationPath _navigationPath = null;
    [SerializeField] private float _turnThreshold = 80f;
    [SerializeField] private bool _randomPatrol = true;

    #region Private Fields

    private int _nextDestination = -1;

    #endregion

    #region State Callbacks

    public override AIStateType GetStateType()
    {
        return AIStateType.Patrol;
    }

    public override void OnEnter()
    {
        // Lets search for a navigation path
        if (_navigationPath == null)
        {
            _navigationPath = FindObjectOfType<AINavigationPath>();
        }

        AISoundManager.SetPlayingSoundType(ESoundType.Roaming);
    }

    public override AIStateType OnUpdate()
    {
        if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type != AITargetType.NavigationPoint && _stateMachine.CurrentTarget.Type != AITargetType.None)
        {
            // We have a target that's not a navigation point, we go into alert mode
            return AIStateType.Alert;
        }

        if ( (!_stateMachine.Agent.hasPath && !_stateMachine.Agent.pathPending)
            || _stateMachine.Agent.isPathStale
            || _stateMachine.Agent.remainingDistance <= _stateMachine.Agent.stoppingDistance)
        {
            SetNewDestination();
        }

        // We should be looking at our next destination
        if (_stateMachine.Agent.hasPath)
        {
            float angleWithDesiredVelocity = Vector3.Angle(transform.forward, _stateMachine.Agent.desiredVelocity);
            if (angleWithDesiredVelocity > _turnThreshold)
            {
                return AIStateType.Alert;
            }
        }

        if (_stateMachine.Agent.desiredVelocity != Vector3.zero)
        {
            Quaternion nextRotation = Quaternion.LookRotation(_stateMachine.Agent.desiredVelocity);
            transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, 10f * Time.deltaTime);
        }

        HandleAnimator();
        HandleRootRotation();

        return AIStateType.Patrol;
    }

    public override void OnExit()
    {
        
    }

    public override void HandleAnimator()
    {
        _stateMachine.Animator.SetInteger("Speed", _stateMachine.Agent.desiredVelocity.magnitude > 0 ? 1 : 0);
    }

    private void HandleRootRotation()
    {
    }

    #endregion

    #region Methods

    private void SetNewDestination()
    {
        if (_navigationPath == null) return;

        if (_navigationPath.NavigationPoints.Count == 0) return;

        if (_randomPatrol)
        {
            _nextDestination = Random.Range(0, _navigationPath.NavigationPoints.Count);
        }
        else
        {
            _nextDestination++;
            if (_nextDestination >= _navigationPath.NavigationPoints.Count) _nextDestination = 0;
        }

        _stateMachine.Agent.SetDestination(_navigationPath.NavigationPoints[_nextDestination].position);

        _stateMachine.SetTarget(_navigationPath.NavigationPoints[_nextDestination], AITargetType.NavigationPoint);
    }

    #endregion
}
