using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CameraController : NetworkBehaviour
{
    [SerializeField] private float _ySensitivity = 150f;
    [SerializeField] private float _min = 317f;
    [SerializeField] private float _max = 60f;
    [SerializeField] private float _lookTransitionSpeed = 7f;

    [SerializeField] private Transform _cameraLookTarget = null;
    [SerializeField] private Transform _aimCameraLookTarget = null;
    [SerializeField] private Transform _crouchCameraLookTarget = null;
    [SerializeField] private Transform _aimCrouchCameraLookTarget = null;

    #region Monobehavior Callbacks

    private void Update()
    {
        if (GameManager.Instance.GameTimeUI.GameStopped) return;

        UpdateYRotation(GameManager.Instance.InputManager.MouseInput.y);
        UpdateLookTarget();
    }

    #endregion

    public void UpdateYRotation(float rotation)
    {
        float eulerAnglesX = transform.rotation.eulerAngles.x - rotation;
        if (eulerAnglesX > 300)
        {
            if (eulerAnglesX < _min)
                eulerAnglesX = _min;
        }
        else if (eulerAnglesX > _max)
        {
            eulerAnglesX = _max;
        }

        Vector3 nextRotationEuler = new Vector3(eulerAnglesX,
            transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        Quaternion nextRotation = Quaternion.Euler(nextRotationEuler);
        transform.rotation = Quaternion.Lerp(transform.rotation, nextRotation, Time.deltaTime * _ySensitivity);
    }

    private void UpdateLookTarget()
    {
        Player _localPlayer = GameManager.Instance.LocalPlayer;

        if (_localPlayer == null) return;

        Transform lookTarget = _cameraLookTarget;

        // Aiming and crouching
        if (_localPlayer.PlayerStates.IsAiming && _localPlayer.PlayerStates.MoveState == PlayerStates.EMoveState.Crouching)
        {
            lookTarget = _aimCrouchCameraLookTarget;
        }

        // Aiming and not crouching
        if (_localPlayer.PlayerStates.IsAiming && _localPlayer.PlayerStates.MoveState != PlayerStates.EMoveState.Crouching)
        {
            lookTarget = _aimCameraLookTarget;
        }

        // Not aiming and crouching
        if (!_localPlayer.PlayerStates.IsAiming && _localPlayer.PlayerStates.MoveState == PlayerStates.EMoveState.Crouching)
        {
            lookTarget = _crouchCameraLookTarget;
        }

        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, lookTarget.position, _lookTransitionSpeed * Time.deltaTime);
    }
}
