using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{
    [SerializeField] private float _hitPoints = 10;
    [SerializeField] float _respawnTime = 5f;
    [SerializeField] ParticleSystem _damageParticleSystem = null;
    [SerializeField] private List<AudioClip> _damageSounds = new List<AudioClip>();
    [SerializeField] private int _killScore = 5;
    [SerializeField] private bool _bodyPartToDecapitate = false;
    [SerializeField][Range(0, 1)] private float _dropItemChance = .5f;

    #region Private Fields

    [SyncVar(hook = "DamageHook")]
    private float _damageTaken = 0f;
    private Player _player = null;

    // To sycn body parts damage
    private NetworkIdentity _networkIdentity = null;
    private AIStateMachine _stateMachine = null;

    #endregion

    private bool _dead = false;

    #region Public Fields

    public event System.Action OnDeath = null;
    public event System.Action OnDamageReceived = null;
    public event System.Action OnReset = null;

    public float HitPointsRemaining
    {
        get
        {
            return _hitPoints - _damageTaken;
        }
    }

    public bool IsAlive
    {
        get
        {
            return HitPointsRemaining > 0;
        }
    }

    public float HitPoints { get { return _hitPoints; } }
    public bool Dead { get { return _dead; } }

    #endregion


    private void Start()
    {
        // Check if it's a player's health. And if it's a player, we deactivate his health if it's not ours
        _player = GetComponent<Player>();
        if (_player != null)
        {
            if (!_player.isLocalPlayer)
            {
                enabled = false;
            }
        }

        // Getting network identity to see if it's a body part or a player/ai.
        _networkIdentity = GetComponent<NetworkIdentity>();
        _stateMachine = GetComponentInParent<AIStateMachine>();
        // If network identity is not found, this is a decapitable body part, so we add it to the list in stateMachine
        if (_stateMachine != null && _networkIdentity == null)
        {
            _stateMachine.AddDecapitableBodyPart(this);
        }
    }

    public virtual void Die()
    {
        if (IsAlive || _dead) return;

        _dead = true;

        if (OnDeath != null) OnDeath();

        // Handle the respawn if it's a player health. Only the server is allowed to handle the respawn
        if (_player != null && GameManager.Instance.LocalPlayer.isServer)
        {
            GameManager.Instance.LocalPlayer.CmdRespawnPlayer(_player.netId);
        }
    }

    // This is only gonna be called by the server affter the shooter calls the take damage (after the raycast)
    public virtual void TakeDamage(float amount, Vector3 hitPoint, uint playerNetId = uint.MaxValue)
    {
        // If the game has stopped, we don't take damage anymore
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        float newDamageTaken = _damageTaken + amount;

        // Blood effect
        if (_damageParticleSystem != null)
        {
            GameObject tmp = Instantiate(_damageParticleSystem.gameObject, hitPoint, Quaternion.identity);
            NetworkServer.Spawn(tmp);
        }

        // Updating the score if we have player (the killer) passed into the function
        if (newDamageTaken >= _hitPoints && playerNetId != uint.MaxValue && !_dead)
        {
            bool updateScore = true;

            if (_bodyPartToDecapitate)
            {
                AIStateMachine stateMachine = GetComponentInParent<AIStateMachine>();
                if (stateMachine != null && stateMachine.Health._dead)
                {
                    updateScore = false;
                }
            }

            int scoreGained = _killScore;

            if (updateScore)
            {
                GameManager.Instance.LeaderboardManager.EnqueueScore(
                    playerNetId,
                    scoreGained);
            }
        }


        // Instantiating pickup items
        // We test if network identity is different than null because we don't want to drop an item when it's a body part
        if (GameManager.Instance.SaveManager.EquippedItems.Count > 0 && newDamageTaken >= _hitPoints && !_dead
            && _networkIdentity != null)
        {
            bool willDrop = Random.Range(0f, 1f) <= _dropItemChance;

            if (willDrop)
            {
                PickupItem item = GameManager.Instance.SaveManager.EquippedItems[Random.Range(0, GameManager.Instance.SaveManager.EquippedItems.Count)];
                if (item != null)
                {
                    PickupItem tmp = Instantiate(item, transform.position + Vector3.up, Quaternion.identity);
                    NetworkServer.Spawn(tmp.gameObject);
                }
            }
        }

        _damageTaken = newDamageTaken;

        // If this isn't a network identity, the _damageTaken variable isn't going to be synchronized
        // And the hook function isn't going to be called
        if (_networkIdentity == null && _stateMachine != null)
        {
            // We need to tell the local player (who has authority) to send an Rpc to all clients for this health to take damage
            GameManager.Instance.LocalPlayer.SyncBodyPartDamage(transform.name, _stateMachine.transform.name, newDamageTaken);
        }

    }

    // This is going to play for everyone when the _damageTaken variable is changed
    public void DamageHook(float oldDamageTaken, float newDamageTaken)
    {
        _damageTaken = newDamageTaken;
        if (OnDamageReceived != null) OnDamageReceived();

        PlayDamageSound();

        if (HitPointsRemaining <= 0) Die();
    }

    public void Reset()
    {
        _dead = false;
        _damageTaken = 0f;
        if (OnReset != null) OnReset();
    }

    public void PlayDamageSound()
    {
        if (_damageSounds.Count > 0)
        {
            AudioClip clip = _damageSounds[Random.Range(0, _damageSounds.Count)];
            if (clip != null)
                GameManager.Instance.AudioManager.PlayOneShotSound(clip, 1, 1, 1, transform.position);
        }
    }
}
