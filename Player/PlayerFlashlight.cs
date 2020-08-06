using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerFlashlight : NetworkBehaviour
{
    [SerializeField] private Material _flashLightLensMaterial = null;
    [SerializeField] private List<Light> _lights = new List<Light>();
    [SerializeField] private AudioClip _toggleSound = null;

    [SyncVar(hook = "HookToggle")]
    private bool _activated = false;

    #region Monobehavior Callback

    private void Start()
    {
        if (!isLocalPlayer) enabled = false;
    }

    private void Update()
    {
        // If game ended, stop doing anything
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        if (GameManager.Instance.InputManager.F && isLocalPlayer)
        {
            CmdToggle();
        }
    }

    [Command]
    private void CmdToggle()
    {
        _activated = !_activated;
    }

    private void HookToggle(bool oldValue, bool newValue)
    {
        if (_activated) 
            _flashLightLensMaterial.EnableKeyword("_EMISSION");
        else
            _flashLightLensMaterial.DisableKeyword("_EMISSION");

        foreach (Light light in _lights)
        {
            light.gameObject.SetActive(_activated);
        }

        if (_toggleSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_toggleSound, 1, 0, 1, transform.position);
        }
    }

    #endregion
}
