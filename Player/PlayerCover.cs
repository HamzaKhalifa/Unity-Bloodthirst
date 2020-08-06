using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerCover : NetworkBehaviour
{
    [SerializeField] int _numberOfRays = 8;

    private bool _canTakeCover = false;
    private bool _isInCover = false;
    private RaycastHit _closestHit;
    private bool _isInCoverAim = false;

    #region Public Callbacks

    public bool IsInCover { get { return _isInCover; } }

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {
        if (!isLocalPlayer) enabled = false;
    }

    private void Update()
    {
        // If game ended, stop doing anything
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        if (!_canTakeCover) return;

        if (GameManager.Instance.LocalPlayer.PlayerStates.IsAiming && _isInCover)
        {
            // We stop being in cover
            _isInCover = false;
            _isInCoverAim = true;
        }

        if (!GameManager.Instance.LocalPlayer.PlayerStates.IsAiming && _isInCoverAim)
        {
            _isInCoverAim = false;
            TakeCover();
        }

        if (GameManager.Instance.InputManager.F)
        {
            if (!_isInCover)
            {
                TakeCover();
            } else
            {
                // Playerstates has access to this field, and playerAnimations also has access to it to set cover to false
                _isInCover = false;
                _isInCoverAim = false;
            }
        }
    }

    #endregion

    private void TakeCover()
    {
        FindCoverAroundPlayer();

        // Now we align the player to the normal of the closes hit
        if (_closestHit.distance != 0)
        {
            // Now get into covering mode
            transform.rotation = Quaternion.LookRotation(_closestHit.normal) * Quaternion.Euler(0, 180, 0);
            _isInCover = true;
        }
    }

    private void FindCoverAroundPlayer()
    {
        _closestHit = new RaycastHit();
        float angleStep = 360 / _numberOfRays;
        for (int i = 0; i < _numberOfRays; i++)
        {
            Quaternion angle = Quaternion.AngleAxis(i * angleStep, Vector3.up);
            CheckClosestPoint(angle);
        }
    }

    private void CheckClosestPoint(Quaternion angle)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position + Vector3.up, angle * Vector3.forward, out hitInfo, 5f, LayerMask.GetMask("Cover")))
        {
            if (/* By default, the closest hit distance is equal to 0*/ _closestHit.distance == 0 || _closestHit.distance > hitInfo.distance)
            {
                _closestHit = hitInfo;
            }
        }
    }

    public void SetPlayerCoverAllowed(bool value)
    {
        _canTakeCover = value;

        if (!_canTakeCover)
        {
            _isInCover = false;
            _isInCoverAim = false;
        }
    }

}
