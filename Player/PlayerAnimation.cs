using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private float _inAirThreshold = .3f;

    #region Animator Hashes

    private int _horizontalHash = Animator.StringToHash("Horizontal");
    private int _verticalHash = Animator.StringToHash("Vertical");
    private int _isWalkingHash = Animator.StringToHash("IsWalking");
    private int _isSprintingHash = Animator.StringToHash("IsSprinting");
    private int _isCrouchingHash = Animator.StringToHash("IsCrouching");
    private int _aimAngleHash = Animator.StringToHash("AimAngle");
    private int _isAimingHash = Animator.StringToHash("IsAiming");
    private int _isCoveringHash = Animator.StringToHash("IsCovering");

    #endregion

    #region Cache Fields

    private Animator _animator = null;
    private PlayerAim _playerAim = null;
    private PlayerAim PlayerAim
    {
        get
        {
            if (_playerAim == null)
                _playerAim = GameManager.Instance.LocalPlayer.PlayerAim;
            return _playerAim;
        }
    }

    private PlayerStates _playerStates = null;
    private Player _player = null;

    #endregion

    #region Private Fields

    private float _inAirTimer = 0f;
    private bool _inAir = false;

    #endregion

    private void Start()
    {
        if (!isLocalPlayer) enabled = false;

        _animator = GetComponentInChildren<Animator>();
        _playerStates = GetComponent<PlayerStates>();
        _animator.SetFloat(_aimAngleHash, 0);
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        // We only want to update the movement animations when the game is playing
        bool gameStopped = GameManager.Instance.GameTimeUI.GameStopped;
        _animator.SetFloat(_horizontalHash, gameStopped ? 0 : GameManager.Instance.InputManager.Horizontal);
        _animator.SetFloat(_verticalHash, gameStopped ? 0 :  GameManager.Instance.InputManager.Vertical);
        _animator.SetBool(_isWalkingHash, GameManager.Instance.InputManager.IsWalking);
        _animator.SetBool(_isSprintingHash, GameManager.Instance.InputManager.IsSprinting);
        _animator.SetBool(_isCrouchingHash, gameStopped ? false : GameManager.Instance.InputManager.IsCrouching);

        _animator.SetFloat(_aimAngleHash, PlayerAim.GetAngle());

        #region Handling Land Animation

        bool isGrounded = GameManager.Instance.LocalPlayer.CharacterController.isGrounded;
        if (!isGrounded)
        {
            _inAirTimer += Time.deltaTime;
            if (_inAirTimer >= _inAirThreshold)
            {
                _inAir = true;
            }
        }
        else {
            _inAirTimer = 0f;
            _inAir = false;
        }
        _animator.SetBool("Landing", _inAir);

        #endregion 

        _animator.SetBool("Handgun", _player.PlayerShoot.ActiveWeapon.GunType == Shooter.EGunType.Handgun);

        // We play the aim animation when the player is aim firing or aiming
        bool isAiming = (GameManager.Instance.LocalPlayer.PlayerStates.WeaponState == PlayerStates.EWeaponState.AimedFiring
            || GameManager.Instance.LocalPlayer.PlayerStates.WeaponState == PlayerStates.EWeaponState.Aiming);

        _animator.SetBool(_isAimingHash, isAiming);

        if (_playerStates != null)
            _animator.SetBool(_isCoveringHash, _playerStates.IsInCover);
    }

    public void SetInAir()
    {
        _inAirTimer += 99f;
    }
}
