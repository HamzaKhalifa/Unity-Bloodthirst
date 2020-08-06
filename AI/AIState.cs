using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AISoundManager))]
public abstract class AIState : MonoBehaviour
{
    public enum AIStateType
    {
        None, Idle, Patrol, Pursuit, Attack, Alert, Feeding, Hurdle
    }

    [SerializeField] protected bool _useRootPosition = true;
    [SerializeField] protected bool _useRootRotation = true;
    [SerializeField] private float _numberOfAnimations = 1;
    [SerializeField] private float _numberOfCrawlAnimations = 1;
    [SerializeField] private string _animationParameter = "";

    #region Cache Fields

    protected AIStateMachine _stateMachine = null;
    public AIStateMachine StateMachine
    {
        set
        {
            _stateMachine = value;
        }
    }

    protected AISoundManager _aiSoundManager = null;
    protected AISoundManager AISoundManager
    {
        get
        {
            if (_aiSoundManager == null)
                _aiSoundManager = GetComponent<AISoundManager>();

            return _aiSoundManager;
        }
    }

    #endregion

    #region Abstract Methods

    public abstract AIStateType GetStateType();
    public abstract void OnEnter();
    public abstract AIStateType OnUpdate();
    public abstract void OnExit();
    public abstract void HandleAnimator();

    #endregion

    #region Monobehvior Callbacks

    public void OnAnimatorUpdated()
    {
        if (_useRootPosition)
            _stateMachine.Agent.velocity = _stateMachine.Animator.deltaPosition / Time.deltaTime;
        if (_useRootRotation)
            transform.rotation = _stateMachine.Animator.rootRotation;
    }

    #endregion

    public void SwitchAnimation()
    {
        if (_stateMachine == null) return;
        float numberOfAnimations = _stateMachine.IsCrawling ? _numberOfCrawlAnimations : _numberOfAnimations;
        float nextAnimationParameterValue = Random.Range(0, (int)numberOfAnimations);
        _stateMachine.Animator.SetFloat(_animationParameter, (float)nextAnimationParameterValue);
    }
}
