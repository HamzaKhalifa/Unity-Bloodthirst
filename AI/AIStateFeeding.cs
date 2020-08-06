using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateFeeding : AIState
{
    [SerializeField] [Range(0, 1)] private float _satisfaction = 1;
    [SerializeField] private float _satisfactionRecoveryRate = .1f;
    [SerializeField] private float _satisfactionDepletionRate = .01f;
    [SerializeField] private float _hungryThreshold = .3f;

    public bool IsHungry
    {
        get
        {
            return _satisfaction <= _hungryThreshold;
        }
    }

    #region Monobehavior Callback

    private void Start()
    {
        _satisfaction = Random.Range(0f, 1f);
    }

    private void Update()
    {
        if (_stateMachine == null) return;

        // If we aren't eating, we should get hungry slowly
        if (_stateMachine.CurrentState != null && _stateMachine.CurrentState.GetStateType() != AIStateType.Feeding)
        {
            _satisfaction -= Time.deltaTime * _satisfactionDepletionRate;
            _satisfaction = Mathf.Max(0, _satisfaction);
        }
    }

    #endregion

    public override AIStateType GetStateType()
    {
        return AIStateType.Feeding;
    }

    public override void OnEnter()
    {
        _stateMachine.Animator.SetBool("IsFeeding", true);
        AISoundManager.SetPlayingSoundType(ESoundType.Feeding);
    }


    public override AIStateType OnUpdate()
    {
        if (_stateMachine.CurrentTarget != null && _stateMachine.CurrentTarget.Type != AITargetType.Food && _stateMachine.CurrentTarget.Type != AITargetType.NavigationPoint && _stateMachine.CurrentTarget.Type != AITargetType.None)
        {
            return AIStateType.Alert;
        }

        // Recover hunger as we eat
        _satisfaction += Time.deltaTime * _satisfactionRecoveryRate;
        _satisfaction = Mathf.Min(1, _satisfaction);

        if (_satisfaction == 1) return AIStateType.Idle;
        
        return AIStateType.Feeding;
    }

    public override void OnExit()
    {
        _stateMachine.Animator.SetBool("IsFeeding", false);
    }

    public override void HandleAnimator()
    {
        
    }
}
