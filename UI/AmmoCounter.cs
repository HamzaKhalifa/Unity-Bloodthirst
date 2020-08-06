using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoCounter : MonoBehaviour
{
    private Text _text = null;

    private void Awake()
    {
        _text = GetComponent<Text>();
    }

    private void Update()
    {
        if (GameManager.Instance.LocalPlayer == null) return;

        Shooter activeWeapon = GameManager.Instance.LocalPlayer.PlayerShoot.ActiveWeapon;
        // Active weapon could be null at the beginning because the switchweapon to the first shooter in the hierarchy is a client RPC that will take some time
        if (activeWeapon != null)
        {
            WeaponReloader weaponReloader = activeWeapon.Reloader;
            if (weaponReloader != null)
            {
                _text.text = weaponReloader.RoundsRemainingInClip + "/" + weaponReloader.RemainingInContainer;
            }
        }
    }
}
