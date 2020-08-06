using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AICrawl : MonoBehaviour
{
    private Health _health = null;

    private void Awake()
    {
        _health = GetComponent<Health>();
        if (_health != null) _health.OnDeath += StartCrawling;
    }

    private void StartCrawling()
    {
        AIStateMachine stateMachine = GetComponentInParent<AIStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.Animator.SetBool("IsCrawling", true);
            stateMachine.IsCrawling = true;
        }
    }
}
