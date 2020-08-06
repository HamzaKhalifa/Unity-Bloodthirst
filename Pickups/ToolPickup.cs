using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolPickup : PickupItem
{
    [SerializeField] private int[] _indexesInPlayerProjectile;
    [SerializeField] private AudioClip _pickupSound = null;

    protected override void Pickup(Transform playerTransform)
    {
        Player player = playerTransform.GetComponent<Player>();

        if (_pickupSound != null) GameManager.Instance.AudioManager.PlayOneShotSound(_pickupSound, 1, 0, 1);

        if (player.gameObject == GameManager.Instance.LocalPlayer.gameObject)
        {
            player.PlayerTools.CmdObtainTools(_indexesInPlayerProjectile);
        }
    }
}
