using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerShoot : NetworkBehaviour
{
    [Header("Sounds")]
    [SerializeField] private AudioClip _switchWeaponSound = null;

    [SerializeField] private List<Shooter> _allShooters = new List<Shooter>();

    [SyncVar]
    private int _currentWeaponIndex = 0;
    private Camera _playerCamera = null;

    #region Public Accessors

    public Shooter ActiveWeapon { get { return _allShooters[_currentWeaponIndex]; } }

    #endregion

    #region Monobehavior Callbacks

    private void Awake()
    {
        // Initializing player camera from which to cast the shooting ray
        _playerCamera = GetComponent<Player>().CameraController.GetComponentInChildren<Camera>();

        // I don't want the weapons to be showing at the start of the game
        foreach(Shooter shooter in _allShooters)
        {
            shooter.HandleEquip(false);
        }
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            Player[] players = FindObjectsOfType<Player>();
            foreach (Player player in players)
            {
                CmdGetShootersStateFromServer(player.netId);
            }
        }
    }

    private void Update()
    {
        // If game ended, stop doing anything
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        for (int i = 0; i < _allShooters.Count; i++)
        {
            _allShooters[i].HandleEquip(_currentWeaponIndex == i);
        }

        if (!isLocalPlayer) return;

        // Handling fire
        if (GameManager.Instance.InputManager.Mouse1
            && GameManager.Instance.LocalPlayer.PlayerStates.MoveState != PlayerStates.EMoveState.Sprinting)
        {
            // We need to first check whether the local player can fire (has ammo in his inventory, is not reloading, etc..)
            Shooter.CheckFireAction checkFireAction = ActiveWeapon.CheckFire();
            if (checkFireAction != Shooter.CheckFireAction.DontFire)
            {
                // Now we call the fire command by the server
                Vector3 fireOrigin = _playerCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2));
                Vector3 fireDirection = _playerCamera.transform.forward;
                CmdFire(checkFireAction, fireOrigin, fireDirection);
            }
        }

        if (GameManager.Instance.InputManager.A)
        {
            SwitchWeapon(1);
        }
        if (GameManager.Instance.InputManager.Z)
        {
            SwitchWeapon(-1);
        }

        if (GameManager.Instance.InputManager.R && ActiveWeapon != null && !ActiveWeapon.Reloader.IsReloading)
        {
            ActiveWeapon.Reloader.Reload();
        }
    }

    #endregion

    #region Synchronizing shooters for new coming clients

    [Command]
    private void CmdGetShootersStateFromServer(uint playerNetId)
    {
        Player[] players = FindObjectsOfType<Player>();
        List<Player> playersList = new List<Player>();
        foreach (Player player in players)
        {
            playersList.Add(player);
        }
        PlayerShoot playerShoot = playersList.Find(player => player.netId == playerNetId).PlayerShoot;

        bool[] inPossession = new bool[_allShooters.Count];
        for (int i = 0; i < playerShoot._allShooters.Count; i++)
        {
            inPossession[i] = playerShoot._allShooters[i].InPossession;
        }

        RpcUpdateShootersStateForEveryone(inPossession, playerNetId);
    }

    [ClientRpc]
    private void RpcUpdateShootersStateForEveryone(bool[] inPossession, uint playerNetId)
    {
        Player[] players = FindObjectsOfType<Player>();
        List<Player> playersList = new List<Player>();
        foreach (Player player in players)
        {
            playersList.Add(player);
        }
        PlayerShoot playerShoot = playersList.Find(player => player.netId == playerNetId).PlayerShoot;

        for (int i = 0; i < playerShoot._allShooters.Count; i++)
        {
            playerShoot._allShooters[i].InPossession = inPossession[i];
        }
    }

    #endregion

    #region Handling fire

    [Command]
    private void CmdFire(Shooter.CheckFireAction checkFireAction, Vector3 fireOrigin, Vector3 fireDirection)
    {
        // We play the fire effects for everyone: Fire sound, reload sound, destablize reticle for the local player, etc..
        RpcFire(checkFireAction);

        // Now we only make the ray cast or instantiate the projective by the server (this one handles all the health points of everyone else)
        if (checkFireAction == Shooter.CheckFireAction.DoFire)
        {
            for(int i = 0; i < ActiveWeapon.NumberOfBulletsFiredAtOnce; i++)
            {
                if (i == 0)
                {
                    ActiveWeapon.Fire(checkFireAction, fireOrigin, fireDirection);
                } else
                {
                    Vector3 direction = (fireDirection * 2) + (Random.insideUnitSphere * ActiveWeapon.Spread);
                    ActiveWeapon.Fire(checkFireAction, fireOrigin, direction);
                }
            }
            
        }

    }

    [ClientRpc]
    private void RpcFire(Shooter.CheckFireAction checkFireAction)
    {
        ActiveWeapon.FireRPC(checkFireAction);
    }

    [ClientRpc]
    public void RpcSpawnBulletHole(Vector3 position, Vector3 direction)
    {
        if (ActiveWeapon != null)
        {
            GameObject bulletHolePrefab = ActiveWeapon.BulletHole;
            GameObject bulletHole = Instantiate(bulletHolePrefab, position, Quaternion.identity);
            bulletHole.transform.forward = direction;
        }
    }

    #endregion

    #region Handling weapon equipping

    private void SwitchWeapon(int direction)
    {
        int nextWeaponIndexInAllShooters = 0;

        // Create a temporary list of weapons in possession
        int activeWeaponIndexInShootersInPossession = 0;
        List<Shooter> shootersInPossession = new List<Shooter>();
        for(int i = 0; i < _allShooters.Count; i++)
        {
            if (_allShooters[i].InPossession) shootersInPossession.Add(_allShooters[i]);
            if(_allShooters[i] == ActiveWeapon)
            {
                activeWeaponIndexInShootersInPossession = shootersInPossession.Count - 1;
            }
        }

        activeWeaponIndexInShootersInPossession += direction;

        if (activeWeaponIndexInShootersInPossession >= shootersInPossession.Count)
            activeWeaponIndexInShootersInPossession = 0;
        if (activeWeaponIndexInShootersInPossession < 0)
            activeWeaponIndexInShootersInPossession = shootersInPossession.Count - 1;

        // Now find the index in all shooters
        for (int i = 0; i < _allShooters.Count; i++)
        {
            if (_allShooters[i] == shootersInPossession[activeWeaponIndexInShootersInPossession])
            {
                nextWeaponIndexInAllShooters = i;
            }
        }

        CmdSwitchWeapon(nextWeaponIndexInAllShooters);

        if (_switchWeaponSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_switchWeaponSound, 1, 0, 1, transform.position);
        }
    }

    [Command]
    private void CmdSwitchWeapon(int nextWeaponIndex)
    {
        _currentWeaponIndex = nextWeaponIndex;
    }

    #endregion

    #region Handling weapon obtention

    [Command]
    public void CmdObtainWeapon(string obtainedShooterName)
    {
        RpcObtainWeapon(obtainedShooterName);
    }

    [ClientRpc]
    public void RpcObtainWeapon(string shooterName)
    {
        foreach (Shooter shooter in _allShooters)
        {
            if (shooter.transform.name == shooterName)
            {
                shooter.InPossession = true;
            }
        }
    }

    #endregion 
}
