using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerProjectile : NetworkBehaviour
{
    [SerializeField] private AudioClip _switchProjectileSound = null;
    [SerializeField] private AudioClip _grenadeSafetyPinSound = null;
    [SerializeField] private AudioClip _throwSound = null;
    [SerializeField] List<EquippedProjectile> _allProjectiles = new List<EquippedProjectile>();

    #region Cache Fields

    private Player _player = null;

    #endregion

    #region private Fields

    private List<EquippedProjectile> _projectiles = new List<EquippedProjectile>();
    private int _selectedProjectileIndex = -1;
    private EquippedProjectile _selectedProjectile = null;

    #endregion

    #region Public Accessors

    public EquippedProjectile SelectedProjectile { get { return _selectedProjectile; } }

    #endregion

    #region Monobehavior Updates

    private void Start()
    {
        _player = GetComponent<Player>();

        foreach (EquippedProjectile projectile in _allProjectiles)
        {
            projectile.PlayerProjectile = this;
        }

        ReInitializeProjectiles();
    }

    private void Update()
    {
        // If game ended, stop doing anything
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        if (!_player.isLocalPlayer) return;

        // We only throw if the selected projectile is not thrown already. 
        if (GameManager.Instance.InputManager.P && _selectedProjectile != null && SelectedProjectile.Coroutine == null)
        {
            _player.NetworkAnimator.SetTrigger(_selectedProjectile.AnimationName);
        }

        if (_projectiles.Count > 1)
        {
            if (GameManager.Instance.InputManager.O)
            {
                CmdSwitchProjectile(1);
            }

            if (GameManager.Instance.InputManager.I)
            {
                CmdSwitchProjectile(-1);
            }
        }
    }

    #endregion

    // This one is called by the player animator event caller after it's called by the animation itself

    [Command]
    public void CmdPlayerThrow(Vector3 throwDestination)
    {
        RpcPlayerThrow(throwDestination);
    }

    [ClientRpc]
    public void RpcPlayerThrow(Vector3 throwDestination)
    {
        if (_selectedProjectile == null) return;

        if (_throwSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_throwSound, 1, GameManager.Instance.LocalPlayer == _player ? 0 : 1, 1, transform.position);
        }

        _selectedProjectile.Throw(throwDestination);

        CmdReInitializeProjectiles();
    }

    #region Projectile Switching

    [Command]
    private void CmdSwitchProjectile(int increment)
    {
        RpcSwitchProjectile(increment);
    }

    [ClientRpc]
    public void RpcSwitchProjectile(int increment)
    {
        _selectedProjectileIndex += increment;

        if (_selectedProjectileIndex >= _projectiles.Count)
            _selectedProjectileIndex = 0;

        if (_selectedProjectileIndex <= -1)
        {
            _selectedProjectileIndex = _projectiles.Count - 1;
        }

        _selectedProjectile = _projectiles[_selectedProjectileIndex];

        if (_player.isLocalPlayer)
            PlaySwitchProjectileSound();
    }

    private void PlaySwitchProjectileSound()
    {
        if (_player == GameManager.Instance.LocalPlayer && _switchProjectileSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_switchProjectileSound, 1, 0, 2, transform.position);
        }
    }

    #endregion

    #region Projectiles in posession reinitilization

    [Command]
    private void CmdReInitializeProjectiles()
    {
        RpcReInitializeProjectiles();
    }

    [ClientRpc]
    private void RpcReInitializeProjectiles()
    {
        ReInitializeProjectiles();
    }

    private void ReInitializeProjectiles()
    {
        _projectiles.Clear();
        foreach (EquippedProjectile projectile in _allProjectiles)
        {
            if (projectile.InPossession)
            {
                _projectiles.Add(projectile);
            }
        }

        if (_projectiles.Count > 0)
        {
            _selectedProjectileIndex = 0;
            _selectedProjectile = _projectiles[_selectedProjectileIndex];
        }
        else
        {
            _selectedProjectileIndex = -1;
            _selectedProjectile = null;
        }
    }

    #endregion

    #region Obtaining Projectile

    [Command]
    public void CmdObtainProjectile(int[] indexes)
    {
        RpcObtainProjectile(indexes);
    }

    [ClientRpc]
    public void RpcObtainProjectile(int[] indexes)
    {
        Debug.Log("Obtaining projectiles");
        // We need to switch projectile if we don't have any projectile yet
        bool switchProjectile = _projectiles.Count == 0;

        foreach (int index in indexes)
        {
            if (_allProjectiles.Count > index)
            {
                _allProjectiles[index].InPossession = true;
                if (!_projectiles.Contains(_allProjectiles[index]))
                    _projectiles.Add(_allProjectiles[index]);
            }
        }

        // If the number of projectiles in possession is now superior to 0, and we previously had nothing, then we switch to the first projectile
        if (switchProjectile && _projectiles.Count > 0)
        {
            _selectedProjectileIndex = 0;
            _selectedProjectile = _projectiles[_selectedProjectileIndex];
        }

    }

    #endregion

    #region Grenade Safety Pin Sound

    [Command]
    public void CmdPlayGrenadeSafetyPinSound()
    {
        RpcPlayGrenadeSafetyPinSound();
    }


    [ClientRpc]
    public void RpcPlayGrenadeSafetyPinSound()
    {
        GameManager.Instance.AudioManager.PlayOneShotSound(_grenadeSafetyPinSound, 1, _player == GameManager.Instance.LocalPlayer == _player ? 0 : 1, 1, transform.position);
    }

    #endregion
}
