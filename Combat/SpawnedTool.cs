using UnityEngine;
using System.Collections;
using Mirror;

public class SpawnedTool : NetworkBehaviour
{
    [SerializeField] private bool _explodeOnTouch = true;
    [SerializeField] private Explosion _explosion = null;
    [SerializeField] private float _throwForce = 300f;
    [SerializeField] private float _explosionDelay = 2f;

    #region Cache Fields

    private Rigidbody _rigidBody = null;

    #endregion

    private uint _throwerIdentifier = 0;
    public uint ThrowerIdentifier { set { _throwerIdentifier = value; } }

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Only the server handles the explosion
        if (!_explodeOnTouch && GameManager.Instance.LocalPlayer.isServer)
        {
            Invoke("Explode", _explosionDelay);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.Instance.LocalPlayer.isServer || !_explodeOnTouch) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("BodyPart")
            || other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            // The player shouldn't collid with his own mines 
            if (other.gameObject.layer == LayerMask.NameToLayer("BodyPart"))
            {
                Player player = other.GetComponentInParent<Player>();
                if (player != null && player.netId == _throwerIdentifier)
                {
                    return;
                }

                // Shouldn't explode when colliding with a body part of a dead enemy
                AIStateMachine stateMachine = other.GetComponentInParent<AIStateMachine>();
                if(stateMachine != null)
                {
                    if (stateMachine.Health.Dead) return;
                }
            }

            Explode();
        }
    }

    public void Explode()
    {
        Explosion tmp = Instantiate(_explosion, transform.position, Quaternion.identity);
        tmp.ThrowerIdentifier = _throwerIdentifier;
        NetworkServer.Spawn(tmp.gameObject, GameManager.Instance.LocalPlayer.connectionToClient);
        Destroy(gameObject);
    }

    public void InitializeThrow(Vector3 throwDestination)
    {
        Vector3 throwDirection = (throwDestination - transform.position).normalized;
        _rigidBody.AddForce(throwDirection * _throwForce);
    }
}
