using UnityEngine;
using System.Collections;

public class PlayerAimSound : MonoBehaviour
{
    [SerializeField] private AudioClip _aimSound = null;

    private PlayerStates.EWeaponState _weaponState = PlayerStates.EWeaponState.None;

    private void Update()
    {
        if (GameManager.Instance.LocalPlayer.PlayerStates.WeaponState == PlayerStates.EWeaponState.Aiming
            && _weaponState != PlayerStates.EWeaponState.Aiming
            && _weaponState != PlayerStates.EWeaponState.AimedFiring
            && _aimSound != null)
        {
            GameManager.Instance.AudioManager.PlayOneShotSound(_aimSound, 1, 0, 1);
        }

        _weaponState = GameManager.Instance.LocalPlayer.PlayerStates.WeaponState;
    }
}
