using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cover : MonoBehaviour
{
    #region Cache Fields

    private Collider _trigger = null;
    public Collider Trigger
    {
        get
        {
            if (_trigger == null)
                _trigger = GetComponentInChildren<Collider>();
            return _trigger;
        }
    }

    private PlayerCover _playerCover = null;
    public PlayerCover PlayerCover
    {
        get
        {
            if (_playerCover == null)
                _playerCover = GameManager.Instance.LocalPlayer.GetComponent<PlayerCover>();

            return _playerCover;
        }
    }

    #endregion

    #region Monobehavior Callbacks

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLocalPlayer(other)) return;

        PlayerCover.SetPlayerCoverAllowed(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLocalPlayer(other)) return;

        PlayerCover.SetPlayerCoverAllowed(false);
    }

    #endregion

    private bool IsLocalPlayer(Collider other)
    {
        if (!other.CompareTag("Player")) return false;

        // We aren't the local player
        if (other.gameObject != GameManager.Instance.LocalPlayer.gameObject) return false;

        return true;
    }
}
