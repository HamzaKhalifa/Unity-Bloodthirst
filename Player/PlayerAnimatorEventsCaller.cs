using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatorEventsCaller : MonoBehaviour
{
    [SerializeField] private PlayerTools _playerTools = null;

    public void PlayerThrow()
    {
        // Only the server handles the throw
        if (!_playerTools.isLocalPlayer) return;

        RaycastHit hitInfo;
        Vector3 throwDestination = Vector3.zero;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(new Vector3((Screen.width / 2f), Screen.height / 2, 5)), out hitInfo, float.MaxValue, LayerMask.GetMask("Default", "Wood", "Metal", "Grass", "Gravel")))
        {
            throwDestination = hitInfo.point;
        }
        else throwDestination = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 999f));

        _playerTools.CmdUseTool(throwDestination);
    }

    public void PlayerUseTool()
    {
        if (!_playerTools.isLocalPlayer) return;

        _playerTools.CmdUseTool(Vector3.zero);
    }

    public void PlayGrenadeSafetyPin()
    {
        _playerTools.PlayGrenadeSafetyPinSound();
    }
}
