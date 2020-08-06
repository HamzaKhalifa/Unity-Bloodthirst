using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoPickup : PickupItem
{
    [SerializeField] private Shooter shooter = null;
    [SerializeField] private EWeaponName _eWeaponType = EWeaponName.None;
    [Tooltip("Amount of ammo. This value is ignored when the shooter gameobject is associated. We give the max ammo of the weapon reloader instead")]
    [SerializeField] private int _amount = 30;
    [SerializeField] private AudioClip _pickupSound = null;

    public Shooter Shooter { get { return shooter;  } }

    private void Start()
    {
        gameObject.SetActive(GameManager.Instance.SaveManager.CheckIfEquipped(shooter));
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Pickup(Transform playerTransform)
    {
        Player player = playerTransform.GetComponent<Player>();

        Container playerInventory = player.Container;
        if (playerInventory != null)
        {
            if (_pickupSound != null) GameManager.Instance.AudioManager.PlayOneShotSound(_pickupSound, 1, 0, 1);

            // Obtain the weapon and put the weapon's max ammo in inventory
            PlayerShoot playerShoot = player.PlayerShoot;
            if(playerShoot != null && shooter != null)
            {
                playerShoot.CmdObtainWeapon(shooter.transform.name);
                playerInventory.Put(_eWeaponType.ToString(), shooter.GetComponent<WeaponReloader>().MaxAmmo);
            } else
            {
                // when this isn't a weapon, but more of a magazin, we put the given ammo in inventory
                playerInventory.Put(_eWeaponType.ToString(), _amount);
            }
        }
    }
}
