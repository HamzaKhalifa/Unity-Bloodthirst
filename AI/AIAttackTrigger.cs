using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIAttackTrigger : MonoBehaviour
{
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _damageDelay = .5f;

    private float _timer = 0f;

    #region Monobehavior Callbacks

    private void Start()
    {
        if (!GameManager.Instance.LocalPlayer.isServer) {
            enabled = false;
        }
    }

    private void Update()
    {
        if (!GameManager.Instance.LocalPlayer.isServer) return;

        _timer += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!GameManager.Instance.LocalPlayer.isServer) return;

        if (_timer >= _damageDelay && other.CompareTag("Player"))
        {
            Health health = other.GetComponent<Health>();
            health.TakeDamage(_damage, transform.position);

            _timer = 0f;
        }
    }

    private void OnEnable()
    {
        _timer = _damageDelay;
    }

    #endregion
}
