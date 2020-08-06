using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemInfo : MonoBehaviour
{
    [SerializeField] protected PickupItem _pickupItem = null;
    [SerializeField] protected Image _itemImage = null;
    [SerializeField] private Image _background = null;
    [SerializeField] protected Text _titleText = null;
    [SerializeField] private Transform _cameraPosition = null;
    [SerializeField] private AudioClip _selectClip = null;
    [SerializeField] private AudioClip _hoverClip = null;
    [SerializeField] private GameObject _equippedSignal = null;

    public Transform CameraPosition { get { return _cameraPosition; } }
    public PickupItem PickupItem { get { return _pickupItem; } }

    private int _itemCategoryIndex = 0;
    private int _itemIndexInCategory = 0;
    private bool _selected = false;
    private bool _haveItem = false;
    private bool _canBuy = false;

    public int ItemCategoryIndex { set { _itemCategoryIndex = value; } }
    public int ItemIndexInCategory { set { _itemIndexInCategory = value; } }

    public bool HaveItem {
        get {
            return ShopPanelsManager.Instance.SaveManager.HaveItem(_pickupItem);
        }
    }

    public bool CanBuy {
        get {
            return ShopPanelsManager.Instance.SaveManager.CanBuy(_pickupItem.Price);
        }
    }

    protected virtual void Awake()
    {
        transform.Find("Container Panel/Price/Price Text").GetComponent<Text>().text = _pickupItem.Price + "$";
    }

    protected virtual void Start()
    {
    }

    public void HandleSelect(bool selected)
    {
        _selected = selected;
        _background.color = selected ? ShopPanelsManager.Instance.SelectedItemInfoColor : ShopPanelsManager.Instance.DeselectedItemInfoColor;
    }

    #region Buttons

    public void HandleHover(bool hovered)
    {
        if (hovered && !_selected)
        {
            AudioManager.Instance.PlayOneShotSound(_hoverClip, 1, 0, 2);
        }

        if (!_selected)
            _background.color = hovered ? ShopPanelsManager.Instance.HoveredItemInfoColor : ShopPanelsManager.Instance.DeselectedItemInfoColor;
    }

    public void ButtonSelect()
    {
        AudioManager.Instance.PlayOneShotSound(_selectClip, 1, 0, 2);
        ShopPanelsManager.Instance.SelectItemWithinCategory(_itemCategoryIndex, _itemIndexInCategory);
    }

    #endregion

    public void HandleEquipSignal(bool equipped)
    {
        _equippedSignal.gameObject.SetActive(equipped);
    }
}
