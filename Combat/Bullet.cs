using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private Explosion _explosion = null;
    [SerializeField] private Transform _explosionPosition = null;

    private uint _throwerIdentifier = 0;
    public uint ThrowerIdentifier
    {
        set { _throwerIdentifier = value; }
    }

    private void Update()
    {
        transform.Translate(transform.forward * _speed, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        Explosion tmp = Instantiate(_explosion, _explosionPosition.position, Quaternion.identity);
        if (GameManager.Instance.LocalPlayer.isServer)
        {
            NetworkServer.Spawn(tmp.gameObject, GameManager.Instance.LocalPlayer.connectionToClient);
        }
        tmp.ThrowerIdentifier = _throwerIdentifier;

        Destroy(gameObject);
    }
}
