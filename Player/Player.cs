using UnityEngine;
using Mirror;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerMove))]
[RequireComponent(typeof(PlayerStates))]
[RequireComponent(typeof(PlayerShoot))]
[RequireComponent(typeof(Health))]
public class Player : NetworkBehaviour
{
    [System.Serializable]
    public class MouseSettings
    {
        public Vector2 Damping = Vector2.zero;
        public Vector2 Sensitivity = Vector2.zero;
    }

    #region Inspector Assigned Fields

    [SerializeField] private bool _lockMouse = true;
    [SerializeField] private float _walkSpeed = 2.5f;
    [SerializeField] private float _crouchSpeed = 1.5f;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 7f;
    [SerializeField] private float _coverSpeed = 2.5f;
    [SerializeField] private MouseSettings _mouseSettings = null;

    [Header("Inspector dependencies")]
    [SerializeField] private PlayerAim _playerAim = null;
    [SerializeField] private SoundEmitter _soundEmitter = null;
    [SerializeField] private CameraController _cameraController = null;
    [SerializeField] private Container _container = null;
    [SerializeField] private Footsteps _footsteps = null;

    [Header("Sounds")]
    [SerializeField] private List<AudioClip> _tauntSounds = new List<AudioClip>();

    #endregion

    #region Cache Fields

    private CharacterController _characterController = null;
    public CharacterController CharacterController
    {
        get
        {
            if (_characterController == null)
                _characterController = GetComponent<CharacterController>();

            return _characterController;
        }
    }

    private PlayerMove _moveController = null;
    public PlayerMove MoveController
    {
        get
        {
            if (_moveController == null)
                _moveController = GetComponent<PlayerMove>();

            return _moveController;
        }
    }

    private Crosshair _crossHair = null;
    public Crosshair MyCrosshair
    {
        get
        {
            if (_crossHair == null)
                _crossHair = FindObjectOfType<Crosshair>();

            return _crossHair;
        }
    }

    private PlayerShoot _playerShoot = null;
    public PlayerShoot PlayerShoot
    {
        get
        {
            if (_playerShoot == null)
            {
                _playerShoot = GetComponent<PlayerShoot>();
            }
            return _playerShoot;
        }
    }

    private PlayerProjectile _playerProjectile = null;
    public PlayerProjectile PlayerProjectile
    {
        get
        {
            if (_playerProjectile == null)
            {
                _playerProjectile = GetComponent<PlayerProjectile>();
            }

            return _playerProjectile;
        }
    }

    private PlayerTools _playerTools = null;
    public PlayerTools PlayerTools
    {
        get
        {
            if (_playerTools == null)
            {
                _playerTools = GetComponent<PlayerTools>();
            }

            return _playerTools;
        }
    }

    public PlayerAim PlayerAim { get { return _playerAim; } }

    private PlayerStates _playerStates = null;
    public PlayerStates PlayerStates
    {
        get
        {
            if (_playerStates == null)
            {
                _playerStates = GetComponent<PlayerStates>();
            }

            return _playerStates;
        }
    }

    private Health _health = null;
    public Health Health
    {
        get
        {
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }

            return _health;
        }
    }

    private NetworkAnimator _networkAnimator = null;
    public NetworkAnimator NetworkAnimator
    {
        get
        {
            if (_networkAnimator == null)
            {
                _networkAnimator = GetComponent<NetworkAnimator>();
            }

            return _networkAnimator;
        }
    }

    public CameraController CameraController { get { return _cameraController; } }
    public Container Container { get { return _container; } }

    #endregion

    #region Private Fields

    private InputManager _inputController = null;
    private Vector2 _mouseInput = Vector2.zero;
    private bool _canMove = true;

    #endregion

    public bool CanMove { get { return _canMove; } set { _canMove = value; } }

    #region Monobehavior Callbacks

    private void Start()
    {
        _inputController = GameManager.Instance.InputManager;

        if (isLocalPlayer)
        {
            InitializeLocalPlayer();
        }
        else
        {
            // Not sure if it's necessary to deactivate the character controller
            // Enemy attacks don't hit anymore when it's deactivated
            //CharacterController.enabled = false;
            enabled = false;
        }
    }

    private void Update()
    {
        // If game ended, stop doing anything
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        if (!Health.IsAlive) return;

        if (_canMove)
            Move();

        LookAround();
    }

    #endregion

    private void InitializeLocalPlayer()
    {
        GameManager.Instance.LocalPlayer = this;

        // We activate all the child player components
        _cameraController.gameObject.SetActive(true);
        _playerAim.gameObject.SetActive(true);
        _container.gameObject.SetActive(true);
        _soundEmitter.SetForLocalPlayer();

        GameManager.Instance.StartCamera.gameObject.SetActive(false);
    }

    #region Private Methods

    private void Move()
    {
        float speed = _runSpeed;

        bool isInCover = false;
        if (_playerStates.MoveState == PlayerStates.EMoveState.Covering) isInCover = true;

        if (_inputController.IsWalking) speed = _walkSpeed;
        if (_inputController.IsCrouching) speed = _crouchSpeed;
        if (_inputController.IsSprinting) speed = _sprintSpeed;
        if (isInCover) speed = _coverSpeed;

        // If we are in cover, we don't move vertically
        float verticalDirection = isInCover ? 0 : _inputController.Vertical * speed;
        Vector2 direction = new Vector2(verticalDirection, _inputController.Horizontal * speed);
        MoveController.Move(direction);
    }

    private void LookAround()
    {
        _mouseInput.x = Mathf.Lerp(_mouseInput.x, _inputController.MouseInput.x, _mouseSettings.Damping.x * Time.deltaTime);
        _mouseInput.y = Mathf.Lerp(_mouseInput.y, _inputController.MouseInput.y, _mouseSettings.Damping.y * Time.deltaTime);

        // We can't rotate the player (or the camera around) if we are in cover
        if (!PlayerStates.IsInCover)
            transform.Rotate(Vector3.up * _mouseInput.x * _mouseSettings.Sensitivity.x);
    }

    [Command]
    public void CmdOnPlayerConnected(string playerName)
    {
        GameManager.Instance.LeaderboardManager.AddPlayer(playerName, this);
    }

    public void SyncBodyPartDamage(string bodyPartName, string stateMachineName, float newDamageTaken)
    {
        CmdSyncBodyPartDamage(bodyPartName, stateMachineName, newDamageTaken);
        //RpcSyncBodyPartDamage(bodyPartName, indexInSpawner, newDamageTaken);
    }

    [Command]
    private void CmdSyncBodyPartDamage(string bodyPartName, string stateMachineName, float newDamageTaken)
    {
        RpcSyncBodyPartDamage(bodyPartName, stateMachineName, newDamageTaken);
    }

    [ClientRpc]
    public void RpcSyncBodyPartDamage(string bodyPartName, string stateMachineName, float newDamageTaken)
    {
        // Server doesn't need to sync the body part damage (it already does it for whatever reason o_O)
        //if (isServer) return;

        Health bodyPartHealth = GameManager.Instance.AISpawner.GetBodyPartHealth(bodyPartName, stateMachineName);
        bodyPartHealth.DamageHook(0, newDamageTaken);
    }

    [Command]
    public void CmdPlayerEmitSound()
    {
        _soundEmitter.transform.position = transform.position;
        _soundEmitter.EmitSound();
        RpcPlayTauntSound();
    }

    [ClientRpc]
    private void RpcPlayTauntSound()
    {
        if (_tauntSounds.Count > 0)
        {
            AudioClip clip = _tauntSounds[Random.Range(0, _tauntSounds.Count)];
            if (clip != null)
            {
                GameManager.Instance.AudioManager.PlayOneShotSound(clip, 1, 1, 1, transform.position);
            }
        }
    }

    [Command]
    public void CmdPlayFootstep()
    {
        RpcPlayFootstep();
    }

    [ClientRpc]
    private void RpcPlayFootstep()
    {
        _footsteps.ActualFootstepPlay();
    }

    #endregion

    #region Handling Start and stop game (for authority)

    [Command]
    public void CmdStartGame()
    {
        GameManager.Instance.GameTimeUI.ServerStartGame();

        RpcStartGame();
    }

    [ClientRpc]
    private void RpcStartGame()
    {
        GameManager.Instance.GameTimeUI.ClientsStartGame();
    }

    [Command]
    public void CmdStopGame()
    {
        GameManager.Instance.GameTimeUI.ServerStopGame();

        RpcStopGame();

        GameManager.Instance.LeaderboardManager.UpdateBestScoreForEachClient();
    }

    [ClientRpc]
    private void RpcStopGame()
    {
        GameManager.Instance.GameTimeUI.ClientsStopGame();
    }

    #endregion

    #region Respawn Methods

    [Command]
    public void CmdInstantPlayerRespawn(uint playerNetId)
    {
        GameManager.Instance.RespawnManager.InstantPlayerRespawn(playerNetId);
    }

    [Command]
    public void CmdRespawnPlayer(uint playerNetId)
    {
        GameManager.Instance.RespawnManager.RespawnPlayer(playerNetId);
    }

    #endregion

}
