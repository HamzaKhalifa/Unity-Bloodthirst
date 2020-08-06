using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerStates : NetworkBehaviour
{
    #region Enumerations

    public enum EMoveState
    {
        None, Walking, Running, Crouching, Sprinting, Covering
    }

    public enum EWeaponState
    {
        None, Idle, Firing, Aiming, AimedFiring
    }

    #endregion 

    public EMoveState MoveState = EMoveState.None;
    public EWeaponState WeaponState = EWeaponState.None;

    #region Cache Fields

    private InputManager _inputController = null;
    private InputManager InputController
    {
        get
        {
            if (_inputController == null)
                _inputController = GameManager.Instance.InputManager;

            return _inputController;
        }
    }

    private PlayerCover _playerCover = null;
    public PlayerCover PlayerCover { get { if (_playerCover == null) _playerCover = GetComponent<PlayerCover>(); return _playerCover; } }

    #endregion

    #region Public Accessors

    public bool IsInCover
    {
        get
        {
            if (PlayerCover != null)
                return PlayerCover.IsInCover;

            return false;
        }
    }

    public bool IsAiming
    {
        get
        {
            return WeaponState == EWeaponState.AimedFiring || WeaponState == EWeaponState.Aiming;
        }
    }

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {
        // If this isn't the local player camera's, we deactivate it.
        if (!isLocalPlayer) enabled = false;
    }

    private void Update()
    {
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        SetMoveState();
        SetWeaponState();
    }

    #endregion

    private void SetMoveState()
    {
        MoveState = EMoveState.Running;

        if (InputController.IsWalking) MoveState = EMoveState.Walking;
        if (InputController.IsCrouching) MoveState = EMoveState.Crouching;
        if (InputController.IsSprinting) MoveState = EMoveState.Sprinting;
        if (IsInCover) MoveState = EMoveState.Covering;
    }

    private void SetWeaponState()
    {
        WeaponState = EWeaponState.Idle;

        if (InputController.Mouse1) WeaponState = EWeaponState.Firing;
        if (InputController.Mouse2) WeaponState = EWeaponState.Aiming;
        if (InputController.Mouse1 && InputController.Mouse2) WeaponState = EWeaponState.AimedFiring;
    }

}
