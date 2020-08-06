using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class AISpawner : MonoBehaviour
{
    [SerializeField] private List<AIStateMachine> _stateMachinesPrefabs = new List<AIStateMachine>();
    [SerializeField] private int _maxZombiesAtATime = 10;
    [SerializeField] private List<Transform> _spawnPositions = new List<Transform>();
    [SerializeField] private float _spawnDelay = 40f;

    private float _spawnTimer = 0f;
    private List<AIStateMachine> _stateMachines = new List<AIStateMachine>();

    #region Monobehavior Updates

    private void Start()
    {
        _spawnTimer = _spawnDelay;
    }

    private void Update()
    {
        // Only the server handles the spawning
        if (GameManager.Instance.LocalPlayer == null) return;
        if (!GameManager.Instance.LocalPlayer.isServer) return;
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        int numberOfAliveStateMachines = 0;
        foreach(AIStateMachine stateMachine in _stateMachines)
        {
            if (stateMachine.Health.IsAlive) numberOfAliveStateMachines++;
        }

        if (numberOfAliveStateMachines <= _maxZombiesAtATime)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnDelay)
            {
                _spawnTimer = 0f;
                AIStateMachine stateMachineToSpawn = _stateMachinesPrefabs[Random.Range(0, _stateMachinesPrefabs.Count)];
                Transform spawnTransfom = _spawnPositions[Random.Range(0, _spawnPositions.Count)];
                if (stateMachineToSpawn != null && spawnTransfom)
                {
                    AIStateMachine tmp = Instantiate(stateMachineToSpawn, spawnTransfom.position, Quaternion.identity);
                    NetworkServer.Spawn(tmp.gameObject, GameManager.Instance.LocalPlayer.connectionToClient);
                }
            }
        }

    }

    #endregion

    #region Public Methods

    public void AddStateMachine(AIStateMachine stateMachine)
    {
        _stateMachines.Add(stateMachine);
    }

    public Health GetBodyPartHealth(string bodyPartName, string stateMachineName)
    {
        foreach(AIStateMachine stateMachine in _stateMachines)
        {
            if (stateMachine == null) continue;

            if (stateMachine.transform.name == stateMachineName)
            {
                return stateMachine.DecapitableBodyParts[bodyPartName];
            }
        }

        return null;
    }

    public void RestartGame()
    {
        // Destroy all the zombies
        foreach(AIStateMachine stateMachine in _stateMachines)
        {
            if (stateMachine != null && stateMachine.gameObject != null)
                Destroy(stateMachine.gameObject);
        }

        _stateMachines.Clear();
    }

    #endregion

}
