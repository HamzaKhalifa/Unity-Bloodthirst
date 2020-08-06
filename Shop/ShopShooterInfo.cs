using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopShooterInfo : ShopItemInfo
{
    [SerializeField] private Text _damageText = null;
    [SerializeField] private Text _recoilText = null;
    [SerializeField] private Text _rateOfFireText = null;

    private Shooter _shooter = null;

    protected override void Awake()
    {
        base.Awake();

        AmmoPickup ammoPickup = (AmmoPickup)_pickupItem;
        _shooter = ammoPickup.Shooter;

        _rateOfFireText = transform.Find("Container Panel/Info Panel/Rate Of Fire Info/Rate Of Fire Text").GetComponent<Text>();
    }

    protected override void Start()
    {
        base.Start();

        CheckEquipped();
        _titleText.text = _shooter.transform.name;
        _damageText.text = _shooter.Damage + "";
        _recoilText.text = _shooter.Recoil + "";
        _rateOfFireText.text = _shooter.RateOfFire + "";
        _itemImage.sprite = _shooter.Sprite;
    }

    private void CheckEquipped()
    {
        HandleEquipSignal(ShopPanelsManager.Instance.CheckIfEquipped(_shooter));
    }
}
