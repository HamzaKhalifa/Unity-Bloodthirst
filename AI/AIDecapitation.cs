using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIDecapitation : MonoBehaviour
{
    [SerializeField] private GameObject _partRenderer = null;
    [SerializeField] private Rigidbody _part = null;
    [SerializeField] private Collider _attack = null;
    [SerializeField] private bool _instantKill = false;

    private bool _decapitated = false;
    private Transform _parent = null;
    private Vector3 _partPosition = Vector3.zero;
    private Quaternion _partRotation = Quaternion.identity;

    #region Public Accessors

    public bool instantKill { get { return _instantKill; } }

    #endregion

    #region Cache Fields

    private Health _health = null;

    #endregion

    private void Start()
    {
        if (_part == null) return;

        _health = GetComponent<Health>();
        if (_health != null) _health.OnDeath += DecapitatePart;

        // Storing the initial values of part for later restoration
        _partPosition = _part.transform.localPosition;
        _partRotation = _part.transform.localRotation;
        _parent = _part.transform.parent;
    }

    public void DecapitatePart()
    {
        if (_partRenderer != null && _part != null && !_decapitated)
        {
            _decapitated = true;
            _part.gameObject.SetActive(true);
            _part.transform.parent = null;

            _partRenderer.SetActive(false);

            if (_attack != null)
            {
                _attack.enabled = false;
            }

            if (_instantKill)
            {
                AIStateMachine stateMachine = GetComponentInParent<AIStateMachine>();
                if (stateMachine != null)
                {
                    Health parentHealth = stateMachine.GetComponentInParent<Health>();
                    if (parentHealth != null)
                    {
                        parentHealth.TakeDamage(10, transform.position);
                    }
                }
            }
        }
    }

    public void Restore()
    {
        if (_part == null) return;

        _decapitated = false;

        _part.transform.parent = _parent;
        _part.transform.localPosition = _partPosition;
        _part.transform.localRotation = _partRotation;
        _part.gameObject.SetActive(false);

        _partRenderer.gameObject.SetActive(true);
    }
}
