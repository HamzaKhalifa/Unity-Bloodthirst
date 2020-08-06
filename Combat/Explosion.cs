using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Explosion : MonoBehaviour
{
    [SerializeField] private float _destructionDelay = 8f;
    [SerializeField] private float _hitDelay = 1f;
    [SerializeField] private float _damage = 5f;

    #region Cache Fields

    private Collider _collider = null;
    private uint _throwerIdentifier = 0;

    #endregion

    #region Public Accessor

    public uint ThrowerIdentifier
    {
        set { _throwerIdentifier = value; }
    }

    #endregion

    #region Monobehavior Fields

    private void Start()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null) _collider.enabled = true;

        Invoke("DisableHit", _hitDelay);
        Invoke("AutoDestroy", _destructionDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.Instance.LocalPlayer.isServer) return;

        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(_damage, other.transform.position + Vector3.up, _throwerIdentifier);
        }
    }

    #endregion

    private void DisableHit()
    {
        if (_collider != null)
            _collider.enabled = false;
    }

    private void AutoDestroy()
    {
        Destroy(gameObject);
    }
}
