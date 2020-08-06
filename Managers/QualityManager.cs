using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualityManager : MonoBehaviour
{
    public static QualityManager instance = null;

    [Header("Enemies")]
    [SerializeField] private bool _optimize = false;
    [SerializeField] private float _optimizationDistance = 10f;
    [SerializeField] private Transform _enemiesParent = null;


    private List<AIStateMachine> _stateMachines = new List<AIStateMachine>();
    private Transform _playerTransform = null;

    private void Start()
    {
        instance = this;

        if (_optimize)
        {
            // Loop through all the enemies and add them one by one to our list if they are activated
            AIStateMachine[] stateMachines;
            if (_enemiesParent != null)
            {
                stateMachines = _enemiesParent.GetComponentsInChildren<AIStateMachine>(true);
            } else
                stateMachines = FindObjectsOfType<AIStateMachine>();

            for (int i = 0; i < stateMachines.Length; i++)
            {
                _stateMachines.Add(stateMachines[i]);

                // Then we deactivate the enemy
                //stateMachines[i].SwitchState(AIState.AIStateType.Idle);
                stateMachines[i].gameObject.SetActive(false);
            }
        }

        GameManager.Instance.OnLocalPlayerJoined += (player) => { _playerTransform = player.transform; };
    }

    private void Update()
    {
        if (_optimize && _playerTransform != null)
        {
            foreach(AIStateMachine stateMachine in _stateMachines)
            {
                if (stateMachine == null) continue;

                // We calculate the distance between the player and the enemy
                float distance = (stateMachine.transform.position - _playerTransform.position).magnitude;
                // Then we activate or deactivate the enemy depending on his distance to the player
                if (distance > _optimizationDistance && stateMachine.gameObject.activeSelf)
                {
                    // We change the zombie to idle before deactivating him only if he isn't dead
                    //if (stateMachine.Health.IsAlive)
                      //  stateMachine.SwitchState(AIState.AIStateType.Idle);
                    stateMachine.gameObject.SetActive(false);
                }
                else if (distance <= _optimizationDistance && !stateMachine.gameObject.activeSelf && stateMachine.Health.IsAlive)
                    stateMachine.gameObject.SetActive(true);
            }
        }
    }
}
