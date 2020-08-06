using UnityEngine;
using System.Collections;

public class ScopeUI : MonoBehaviour
{
    [SerializeField] private GameObject _scopePanel = null;
    [SerializeField] private float _scopeDelay = .5f;

    private bool _scopeActive = false;
    private IEnumerator _coroutine = null;
    private float _initialCameraFOV = 60f;

    private void Start()
    {
        _initialCameraFOV = Camera.main.fieldOfView;
    }

    private void Update()
    {
        if (GameManager.Instance.LocalPlayer.PlayerStates.IsAiming &&
            GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon != null &&
            GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.HasScope &&
            !_scopeActive)
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = ActivateScope();
            StartCoroutine(_coroutine);
        }

        if (_scopeActive &&
            (!GameManager.Instance.LocalPlayer.PlayerStates.IsAiming ||
            GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon == null ||
            (GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon != null && !GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.HasScope)))
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = null;
            DeactivateScope();
        }
    }

    private IEnumerator ActivateScope()
    {
        _scopeActive = true;

        yield return new WaitForSeconds(_scopeDelay);

        GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.HandleMeshActivation(false);
        _scopePanel.gameObject.SetActive(true);
        Camera.main.fieldOfView = _initialCameraFOV - GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.Zoom;

        // Playing scope sound
        if(GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.ScopeSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.ScopeSound,
                1, 0, 1, transform.position);
        }
    }

    private void DeactivateScope()
    {
        GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.HandleMeshActivation(true);
        _scopeActive = false;
        _scopePanel.gameObject.SetActive(false);
        Camera.main.fieldOfView = _initialCameraFOV;
    }
}
