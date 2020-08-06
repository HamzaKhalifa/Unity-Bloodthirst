using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public enum AITargetType
{
    None, NavigationPoint, Sound, Food, Player
}

public class AITarget
{
    public AITargetType Type = AITargetType.None;
    public Vector3 LastSeenPosition = Vector3.zero;
    public Transform TargetTransform = null;
}

[RequireComponent(typeof(AIStateIdle))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(AISoundManager))]
[RequireComponent(typeof(AIStateIdle))]
[RequireComponent(typeof(AIStatePatrol))]
[RequireComponent(typeof(AIStateAlert))]
[RequireComponent(typeof(AIStatePursuit))]
[RequireComponent(typeof(AIStateAttack))]
[RequireComponent(typeof(AIStateFeeding))]
public class AIStateMachine : NetworkBehaviour
{
    #region Inspector Assigned Fields

    [Header("Testing")]
    [SerializeField] private string _targetName = null;

    [SerializeField] private GameObject _food = null;

    [SerializeField] private float _screamingPeriod = 20f;

    [SerializeField] private AIScanner _aiScanner = null;

    #endregion

    #region Cache Fields

    private Animator _animator = null;
    public Animator Animator
    {
        get
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();

            return _animator;
        }
    }

    private NavMeshAgent _agent = null;
    public NavMeshAgent Agent
    {
        get
        {
            if (_agent == null)
                _agent = GetComponent<NavMeshAgent>();

            return _agent;
        }
    }

    private Health _health = null;
    public Health Health
    {
        get
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
                _health.OnDamageReceived += () => { if(_health.IsAlive) AISoundManager.PlaySound(ESoundType.Damage); };
                _health.OnDeath += () => {
                    AISoundManager.SetPlayingSoundType(ESoundType.None);
                    if (_food != null)
                    {
                        _food.SetActive(true);
                    }

                    AIState aIState = _statesDictionary[AIState.AIStateType.Attack];
                    AIStateAttack aIStateAttack = (AIStateAttack)aIState;
                    aIStateAttack.DeactivateAllAttacks();
                };
            }

            return _health;
        }
    }

    private AISoundManager _aiSoundManager = null;
    public AISoundManager AISoundManager
    {
        get
        {
            if (_aiSoundManager == null)
                _aiSoundManager = GetComponent<AISoundManager>();

            return _aiSoundManager;
        }
    }

    public AIState GetState(AIState.AIStateType stateType)
    {
        return _statesDictionary[stateType];
    }
    #endregion

    #region Private Fields

    private bool _isCrawling = false;
    private AIState _currentState = null;
    private Dictionary<AIState.AIStateType, AIState> _statesDictionary = new Dictionary<AIState.AIStateType, AIState>();
    private AITarget _currentTarget = null;
    private float _nextScreamTime = 0f;
    // Synching layer weights since mirror doesn't seem to handle them
    [SyncVar(hook = "HookHandleAlertLayer")]
    private float _alertLayerWeight = 0;
    private Dictionary<string, Health> _decapitableBodyParts = new Dictionary<string, Health>();
    [SyncVar(hook = "HookChangeName")]
    private string _name = "";

    #endregion

    #region Public Accessors

    public AITarget CurrentTarget { get { return _currentTarget; } set { _currentTarget = value; } }
    public bool IsCrawling { get { return _isCrawling; } set { _isCrawling = value; } }
    public AIState CurrentState { get { return _currentState; } }
    public Dictionary<string, Health> DecapitableBodyParts { get { return _decapitableBodyParts; } }

    #endregion

    #region Monobehvior Callbacks

    private void Start()
    {
        // We need to keep our state machines registered somewhere
        GameManager.Instance.AISpawner.AddStateMachine(this);

        _aiScanner.gameObject.SetActive(isServer);
        Agent.enabled = isServer;
        if (!isServer) return;

        // We need to generate a random name for the state machine in order to properly sync the decapitation of parts between clients
        _name = Bloodthirst.Utils.GenerateRandomId(10);

        // Getting all the states first
        AIState[] states = GetComponents<AIState>();
        foreach(AIState state in states)
        {
            _statesDictionary.Add(state.GetStateType(), state);
            state.StateMachine = this;
            state.SwitchAnimation();
        }

        Health.OnDeath += OnDeath;

        // We go into idle state mode by default
        SwitchState(AIState.AIStateType.Idle);
    }

    private void Update()
    {
        if (!isServer) return;

        // Just for testing
        _targetName = "";
        if (_currentTarget != null && _currentTarget.TargetTransform != null)
        {
            _targetName = _currentTarget.TargetTransform.name;
        } 

        if (_currentState == null) return;

        if (!Health.IsAlive) return;

        AIState.AIStateType nextStateType = _currentState.OnUpdate();

        if (nextStateType != _currentState.GetStateType()) SwitchState(nextStateType);
    }

    private void OnAnimatorMove()
    {
        if (!isServer) return;

        if (_currentState == null) return;

        _currentState.OnAnimatorUpdated();
    }

    #endregion

    #region Methods

    public void SwitchState(AIState.AIStateType stateType)
    {
        if (_currentState != null) _currentState.OnExit();

        if (_statesDictionary.TryGetValue(stateType, out _currentState))
        {
            // No need to work on changing layers when we go into alert mode
            if (stateType == AIState.AIStateType.Attack
                || stateType == AIState.AIStateType.Pursuit)
            {
                _alertLayerWeight = 1;
                Animator.SetLayerWeight(Animator.GetLayerIndex("Alert Layer"), _alertLayerWeight);
            }
            else if (stateType == AIState.AIStateType.Feeding || stateType == AIState.AIStateType.Patrol
                || stateType == AIState.AIStateType.Idle)
            {
                _alertLayerWeight = 0;
                Animator.SetLayerWeight(Animator.GetLayerIndex("Alert Layer"), _alertLayerWeight);
            }

            _currentState.OnEnter();
        } else
        {
            // If we don't find the state, we go into Idle.
            SwitchState(AIState.AIStateType.Idle);
        }
    }

    public void SetTarget(Transform transform, AITargetType targetType)
    {
        AITarget target = new AITarget();
        target.TargetTransform = transform;
        target.LastSeenPosition = transform.position + Vector3.up;
        target.Type = targetType;

        _currentTarget = target;
    }

    public void ResetTarget()
    {
        _currentTarget = null;
    }

    private void OnDeath()
    {
        Agent.ResetPath();
        Agent.enabled = false;
    }

    public void AddDecapitableBodyPart(Health bodyPartHealth)
    {
        _decapitableBodyParts.Add(bodyPartHealth.transform.name, bodyPartHealth);
    }

    #endregion

    #region Hooks

    private void HookHandleAlertLayer(float oldValue, float newValue)
    {
        Animator.SetLayerWeight(Animator.GetLayerIndex("Alert Layer"), newValue);

        if (_currentTarget != null
                    && _currentTarget.Type == AITargetType.Player
                    && Time.time >= _nextScreamTime
                    && !_isCrawling)
        {
            // We scream here
            Animator.SetTrigger("Scream");
            _nextScreamTime = Time.time + _screamingPeriod;
        }
    }

    private void HookChangeName(string oldName, string newName)
    {
        transform.name = newName;
    }

    #endregion
}
