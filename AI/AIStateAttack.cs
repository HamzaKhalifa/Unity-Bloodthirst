using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIStateAttack : AIState
{
    [SerializeField] private float _followRotationSpeed = 10f;
    [SerializeField] private List<Collider> _attacks = new List<Collider>();

    public override AIStateType GetStateType()
    {
        return AIStateType.Attack;
    }

    public override void OnEnter()
    {
        _stateMachine.Animator.SetInteger("Speed", 0);
        _stateMachine.Animator.SetBool("IsAttacking", true);
    }

    public override AIStateType OnUpdate()
    {
        // If the target disappears, we go into alert mode
        if (_stateMachine.CurrentTarget == null) return AIStateType.Alert;

        if (_stateMachine.Agent.isPathStale)
        {
            return AIStateType.Hurdle;
        }

        // If the target gets away, we go into pursuit mode
        if (Vector3.Distance(transform.position, _stateMachine.CurrentTarget.TargetTransform.position) > _stateMachine.Agent.stoppingDistance)
        {
            return AIStateType.Pursuit;
        }

        // Keeping the objective in sight
        Quaternion nextRotation = Quaternion.LookRotation(_stateMachine.CurrentTarget.TargetTransform.position - transform.position);
        nextRotation.eulerAngles = new Vector3(transform.rotation.eulerAngles.x, nextRotation.eulerAngles.y, nextRotation.eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, _followRotationSpeed * Time.deltaTime);

        return AIStateType.Attack;
    }

    public override void OnExit()
    {
        _stateMachine.Animator.SetBool("IsAttacking", false);
    }

    public override void HandleAnimator()
    {
        
    }

    public void Attack(int attackIndex)
    {
        if (_attacks.Count > attackIndex)
        {
            _attacks[attackIndex].gameObject.SetActive(true);
        }
    }

    public void StopAttack(int attackIndex)
    {
        if (_attacks.Count > attackIndex)
        {
            _attacks[attackIndex].gameObject.SetActive(false);
        }
    }

    public void DeactivateAllAttacks()
    {
        foreach(Collider attack in _attacks)
        {
            attack.gameObject.SetActive(false);
        }
    }
}
