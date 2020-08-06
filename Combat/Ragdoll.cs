using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Ragdoll : MonoBehaviour
{
    [SerializeField] private List<Rigidbody> _bodyParts = new List<Rigidbody>();
    private bool _ragdolled = false;

    #region Cache Fields

    private Health _health = null;
    public Health Health
    {
        get
        {
            if (_health == null)
                _health = GetComponent<Health>();
            return _health;
        }
    }

    private Animator _animator = null;
    public Animator Animator
    {
        get
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                    _animator = GetComponentInChildren<Animator>();
            }

            return _animator;
        }
    }

    #endregion

    #region Public Accessors

    public List<Rigidbody> BodyParts { get { return _bodyParts; } }

    #endregion

    #region Monobehavior Callbacks

    private void Awake()
    {
        //Health.OnDeath += ExecuteRagdoll;
        //Health.OnReset += DeRagdoll;
    }

    private void Update()
    {
        if (Health.IsAlive && _ragdolled)
        {
            DeRagdoll();
        }

        if (!Health.IsAlive && !_ragdolled)
        {
            ExecuteRagdoll();
        }
    }

    #endregion

    private void ExecuteRagdoll()
    {
        if (Animator != null)
            Animator.enabled = false;

        foreach(Rigidbody bodyPart in _bodyParts)
        {
            bodyPart.isKinematic = false;
            bodyPart.GetComponent<Collider>().isTrigger = false;
        }

        _ragdolled = true;
    }

    private void DeRagdoll()
    {
        foreach (Rigidbody bodyPart in _bodyParts)
        {
            bodyPart.isKinematic = true;
            bodyPart.GetComponent<Collider>().isTrigger = true;
        }

        if (Animator != null)
            Animator.enabled = true;

        _ragdolled = false;
    }
}
