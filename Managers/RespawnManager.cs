using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RespawnManager : NetworkBehaviour
{
    public enum ERespawPositionType
    {
        Self, Determined
    }

    [System.Serializable]
    public class RespawnSettings
    {
        public bool Respawn = false;
        public ERespawPositionType RespawnPositionType = ERespawPositionType.Self;
        public List<Transform> RespawnPositions = new List<Transform>();
    }

    [SerializeField] private List<Transform> _playerRespawnPositions = new List<Transform>();
    [SerializeField] private float _playerRespawnTime = 5f;

    public void InstantPlayerRespawn(uint playerNetId)
    {
        Vector3 respawnPosition = _playerRespawnPositions[Random.Range(0, _playerRespawnPositions.Count)].position;
        RpcRespawn(playerNetId, respawnPosition);
    }

    public void RespawnPlayer(uint playerNetId)
    {
        Vector3 respawnPosition = _playerRespawnPositions[Random.Range(0, _playerRespawnPositions.Count)].position;
        GameManager.Instance.TimerManager.Add(() =>
        {
            RpcRespawn(playerNetId, respawnPosition);
        }, _playerRespawnTime);
    }

    [ClientRpc]
    private void RpcRespawn(uint playerNetworkId, Vector3 respawnPosition)
    {
        Player[] players = FindObjectsOfType<Player>();
        // Find the player with the given id first
        foreach(Player player in players)
        {
            if (player.netId == playerNetworkId)
            {
                player.CharacterController.enabled = false;
                player.transform.position = respawnPosition;
                player.Health.Reset();
                player.CharacterController.enabled = true;

                break;
            }
        }
    }

    public void Add(Health health, float inSeconds, RespawnSettings respawnSettings)
    {
        if (respawnSettings.Respawn)
        {
            health.gameObject.SetActive(false);
            GameManager.Instance.TimerManager.Add(() =>
            {
                if (respawnSettings.RespawnPositionType == ERespawPositionType.Determined
                 && respawnSettings.RespawnPositions.Count > 0)
                {
                    health.transform.position = respawnSettings.RespawnPositions[Random.Range(0, respawnSettings.RespawnPositions.Count)].position;
                }
                health.gameObject.SetActive(true);
            }, inSeconds);
        }
    }
}
