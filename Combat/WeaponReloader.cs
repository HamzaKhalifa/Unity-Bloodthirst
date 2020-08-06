using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponReloader : MonoBehaviour
{
    [SerializeField] int _maxAmmo = 400;
    [SerializeField] int _clipSize = 30;
    [SerializeField] Container _inventory = null;
    [SerializeField] EWeaponName _eWeaponType = EWeaponName.None;
    [Header("Sounds")]
    [SerializeField] private AudioClip _reloadSound = null;

    #region Private Fields

    private int _shotsFiredInClip = 0;
    private bool _isReloading = false;
    private System.Guid _containerItemId = System.Guid.Empty;

    #endregion

    #region Public Accessors

    public int RoundsRemainingInClip
    {
        get
        {
            return Mathf.Max(0, _clipSize - _shotsFiredInClip);
        }
    }

    public bool IsReloading { get { return _isReloading; } }
    public int RemainingInContainer { get { return _inventory.RemainingById(_containerItemId); } }
    public int MaxAmmo { get { return _maxAmmo; } }

    #endregion

    #region Monobehavior Callbacks

    private void Awake()
    {
        _containerItemId = _inventory.Add(_eWeaponType.ToString(), _maxAmmo);
    }

    #endregion

    #region Methods

    public bool Reload()
    {
        if (_isReloading) return false;

        _isReloading = true;

        int amountFromInventory = _inventory.TakeFromContainer(_containerItemId, _shotsFiredInClip);

        // If there is no more ammo or the clip is full, we do nothing
        if (amountFromInventory == 0) { _isReloading = false; return false; }

        GameManager.Instance.SetLoadingIndicator(true);

        if (_reloadSound != null)
            GameManager.Instance.AudioManager.PlayOneShotSound(_reloadSound, 1, 1, 1, transform.position);

        // Reload time is equal to the reload clip length, otherwise, it's set to 2 by default
        float reloadTime = _reloadSound != null ? _reloadSound.length : 2f;

        GameManager.Instance.TimerManager.Add(() => {
            ExecuteReload(amountFromInventory);
            GameManager.Instance.SetLoadingIndicator(false);
        }, reloadTime);

        return true;
    }

    private void ExecuteReload(int amount)
    {
        _isReloading = false;

        _shotsFiredInClip -= amount;
    }

    public void TakeFromClip(int amount)
    {
        _shotsFiredInClip += amount;
    }

    #endregion

}
