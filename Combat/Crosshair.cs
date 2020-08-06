using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    #region Cache Fields

    private Transform _reticle = null;
    private RectTransform _top = null;
    private RectTransform _bottom = null;
    private RectTransform _right = null;
    private RectTransform _left = null;

    #endregion

    #region Private Fields

    private IEnumerator _destablizeCoroutine = null;
    private Shooter _activeWeapon = null;

    #endregion

    #region Monobehavior Callbacks

    private void Start()
    {
        InitializeReticle();
    }

    private void Update()
    {
        if (_reticle == null || GameManager.Instance.LocalPlayer == null) return;

        // If we don't have a weapon equipped, we turn off the reticle.
        _activeWeapon = GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon;
        if (_activeWeapon == null && _reticle.gameObject.activeSelf) _reticle.gameObject.SetActive(false);
        if (_activeWeapon != null && !_reticle.gameObject.activeSelf) _reticle.gameObject.SetActive(true);

        // Updating the aiming acccuracy
        // We only recover the reticle when aren't destablizing it
        if (_destablizeCoroutine == null && _activeWeapon != null)
        {
            float recoverySpeed = GameManager.Instance.LocalPlayer.PlayerStates.IsAiming ? _activeWeapon.AimReticleRecoverySpeed : _activeWeapon.ReticleRecoverySpeed;
            float inaccuracy = GameManager.Instance.LocalPlayer.PlayerStates.IsAiming ? _activeWeapon.AimReticleInaccuracy : _activeWeapon.ReticleInaccuracy;
            RecoverReticle(inaccuracy, recoverySpeed);
        }
    }

    #endregion

    private void InitializeReticle()
    {
        _reticle = transform.Find("Reticle");

        if (_reticle == null) return;

        _top = _reticle.transform.Find("Top").GetComponent<RectTransform>();
        _bottom = _reticle.transform.Find("Bottom").GetComponent<RectTransform>();
        _right = _reticle.transform.Find("Right").GetComponent<RectTransform>();
        _left = _reticle.transform.Find("Left").GetComponent<RectTransform>();
    }

    private void RecoverReticle(float accuracy, float aimRecoverySpeed)
    {
        if (_top == null) return;

        float nextPosition = Mathf.Lerp(_top.localPosition.y, accuracy, aimRecoverySpeed * Time.deltaTime);

        UpdateReticle(nextPosition);
    }

    private void UpdateReticle(float nextPosition)
    {
        if (_top == null || _right == null || _left == null || _bottom == null) return;

        _top.localPosition = new Vector3(_top.localPosition.x, nextPosition, _top.localPosition.z);
        _bottom.localPosition = new Vector3(_bottom.localPosition.x, -nextPosition, _bottom.localPosition.z);
        _right.localPosition = new Vector3(nextPosition, _right.localPosition.y, _right.localPosition.z);
        _left.localPosition = new Vector3(-nextPosition, _left.localPosition.y, _left.localPosition.z);
    }

    public void Destabilize()
    {
        // Exerting recoil
        GameManager.Instance.LocalPlayer.CameraController.UpdateYRotation(GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon.Recoil);

        if (_destablizeCoroutine != null) {
            StopCoroutine(_destablizeCoroutine);
            _destablizeCoroutine = null;
        }

        _destablizeCoroutine = DestabilizeCoroutine();
        StartCoroutine(_destablizeCoroutine);
    }

    private IEnumerator DestabilizeCoroutine()
    {
        float delay = _activeWeapon.DetabilizationSpeed;
        if (GameManager.Instance.LocalPlayer.PlayerStates.IsAiming)
            delay = _activeWeapon.AimDetabilizationSpeed;

        float time = 0f;

        float initialPosition = _top.localPosition.y;

        // When aiming, instability should be halved
        float instability = _activeWeapon.Instability;
        if (GameManager.Instance.LocalPlayer.PlayerStates.IsAiming) instability = _activeWeapon.AimInstability;

        while (time < delay)
        {
            time += Time.deltaTime;
            float normalizedTime = time / delay;

            float nextPosition = Mathf.Clamp(initialPosition + (instability * normalizedTime), 0, _activeWeapon.MaxInstability);
            UpdateReticle(nextPosition);

            yield return null;
        }

        UpdateReticle(initialPosition + instability);

        _destablizeCoroutine = null;
    }
}
