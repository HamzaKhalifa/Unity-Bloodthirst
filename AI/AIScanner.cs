using UnityEngine;

public class AIScanner : MonoBehaviour
{
    [SerializeField] private float _fieldOfView = 90f;

    #region Cache Fields

    private AIStateMachine _stateMachine = null;
    private SphereCollider _sphereCollider = null;

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {
        _stateMachine = GetComponentInParent<AIStateMachine>();
        _sphereCollider = GetComponent<SphereCollider>();
    }

    private void OnTriggerStay(Collider other)
    {
        bool playerInSight = false;
        bool foodInSight = false;

        if (other.CompareTag("Player") || other.CompareTag("Food"))
        {
            RaycastHit hitInfo;
            float upCoeffecient = other.CompareTag("Player") ? 1 : 0;
            Debug.DrawRay(transform.position, (other.transform.position + Vector3.up * upCoeffecient) - transform.position, Color.red);
            if (Physics.Raycast(transform.position, (other.transform.position + Vector3.up * upCoeffecient)  - transform.position, out hitInfo, _sphereCollider.radius, LayerMask.GetMask("EnemyThreat", "Default", "Gravel", "Wood", "Grass", "Metal")))
            {
                if (hitInfo.transform.gameObject == other.gameObject)
                {
                    float angle = Vector3.Angle(transform.forward, (other.transform.position + Vector3.up * upCoeffecient) - transform.position);
                    if (angle <= _fieldOfView)
                    {
                        // We have the player directly in sight
                        if (other.CompareTag("Player"))
                        {
                            _stateMachine.SetTarget(other.transform, AITargetType.Player);
                            playerInSight = true;
                        }

                        if (other.CompareTag("Food"))
                        {
                            // We only start feeding when there is no other target in sight
                            // or when the target is neither a player neither a sound
                            if (_stateMachine.CurrentTarget == null
                                || (_stateMachine.CurrentTarget != null &&
                                _stateMachine.CurrentTarget.Type != AITargetType.Player
                                && _stateMachine.CurrentTarget.Type != AITargetType.Sound))
                            {
                                // We need to check if we are hungry
                                AIState state = _stateMachine.GetState(AIState.AIStateType.Feeding);
                                if (state != null)
                                {
                                    AIStateFeeding stateFeeding = (AIStateFeeding)state;
                                    if (stateFeeding.IsHungry)
                                    {
                                        _stateMachine.SetTarget(other.transform, AITargetType.Food);
                                        foodInSight = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // We shouldn't detect our own sound
        if (other.CompareTag("Sound") && _stateMachine.AISoundManager.SoundEmitter.gameObject != other.gameObject)
        {
            if (_stateMachine.CurrentTarget == null
                || _stateMachine.CurrentTarget != null &&
                    (_stateMachine.CurrentTarget.Type == AITargetType.Food || _stateMachine.CurrentTarget.Type == AITargetType.NavigationPoint
                    || _stateMachine.CurrentTarget.Type == AITargetType.None))
            {
                _stateMachine.SetTarget(other.transform, AITargetType.Sound);
            }
        }

        if(_stateMachine.CurrentTarget != null)
        {
            // If we were seeing the player and no longer see him, then we reset the target
            if (other.CompareTag("Player") && !playerInSight && _stateMachine.CurrentTarget.Type == AITargetType.Player)
            {
                _stateMachine.ResetTarget();
            }

            // If we aren't seeing the player nor are we seeing the food and the previous target was food, then we reset the target
            // Food is the least prioritary out of all targets
            if (other.CompareTag("Food") && !foodInSight && _stateMachine.CurrentTarget.Type == AITargetType.Food)
            {
                _stateMachine.ResetTarget();
            }
        }
        
    }

    #endregion
}
