using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShopPanelsManager : MonoBehaviour
{
    public static ShopPanelsManager Instance = null;

    #region Helper Classes

    [System.Serializable]
    public class ShopItemCategory
    {
        public List<ShopItemInfo> ShopItemsInfos = new List<ShopItemInfo>();
        public int SelectedItem = 0;
    }

    #endregion

    [SerializeField] private SaveManager _saveManager = null;
    [SerializeField] private List<ShopItemCategory> _shopItemsCategories = new List<ShopItemCategory>();
    [SerializeField] private float _cameraSwitchSpeed = 10f;
    [SerializeField] private Color _selectedItemInfoColor = new Color();
    [SerializeField] private Color _hovererItemInfoColor = new Color();
    [SerializeField] private Color _deselectedItemInfoColor = new Color();
    [SerializeField] private AudioClip _equipSound = null;
    [SerializeField] private Button _equipButton = null;
    [SerializeField] private Button _buyButton = null;

    [SerializeField] private Text _cashAmountText = null;
    [SerializeField] private AudioClip _cashSound = null;
    [SerializeField] private AudioClip _notEnoughCashSound = null;

    [Header("For categories wheel")]
    [SerializeField] private List<Image> _categoriesButtonsImages = new List<Image>();
    [SerializeField] private Color _selectedCategoryColor = new Color();
    [SerializeField] private Color _deSelectedCategoryColor = new Color();
    [SerializeField] private HorizontalLayoutGroup _wheel = null;
    [SerializeField] private int _step = 420;
    [SerializeField] private AudioClip _categorySwitchSound = null;

    #region Public Accessors

    public Color SelectedItemInfoColor { get { return _selectedItemInfoColor; } }
    public Color HoveredItemInfoColor { get { return _hovererItemInfoColor; } }
    public Color DeselectedItemInfoColor { get { return _deselectedItemInfoColor; } }
    public SaveManager SaveManager { get { return _saveManager; } }

    #endregion

    #region Private Fields

    private int _selectedCategory = 0;

    #endregion

    #region Monobehavior Callbacks

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SelectCategory(0);

        // For each item category, we select the first item
        for (int i = 0; i < _shopItemsCategories.Count; i++)
        {
            SelectItemWithinCategory(i, 0);

            // We initialize our items too
            for (int j = 0; j < _shopItemsCategories[i].ShopItemsInfos.Count; j++)
            {
                _shopItemsCategories[i].ShopItemsInfos[j].ItemCategoryIndex = i;
                _shopItemsCategories[i].ShopItemsInfos[j].ItemIndexInCategory = j;
            }
        }
    }

    private void Update()
    {
        ShopItemInfo currentItemInfo = _shopItemsCategories[_selectedCategory].ShopItemsInfos[_shopItemsCategories[_selectedCategory].SelectedItem];

        // Managing our camera
        Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position,
            currentItemInfo.CameraPosition.position,
            _cameraSwitchSpeed * Time.deltaTime);

        // Managing categories wheel
        int targetLeftPadding = -(-10 + _selectedCategory * _step);
        int nextLeftPadding = (int)Mathf.Lerp(_wheel.padding.left, targetLeftPadding, Time.deltaTime * 10);
        _wheel.padding = new RectOffset(nextLeftPadding,
            _wheel.padding.right, _wheel.padding.top, _wheel.padding.bottom);
    }

    #endregion

    #region Navigation and equipment

    public void SelectItemWithinCategory(int categoryIndex, int itemIndex)
    {
        // We first deselect all the items
        foreach(ShopItemInfo shopItemInfo in _shopItemsCategories[categoryIndex].ShopItemsInfos)
        {
            shopItemInfo.HandleSelect(false);
        }

        // Now we select our target item
        ShopItemInfo selectedShopItemInfo = _shopItemsCategories[categoryIndex].ShopItemsInfos[itemIndex];
        selectedShopItemInfo.HandleSelect(true);
        _shopItemsCategories[categoryIndex].SelectedItem = itemIndex;

        // Now handling the buy and equip button
        HandleEquipAndBuyButton();
    }

    private void HandleEquipAndBuyButton()
    {
        ShopItemInfo currentItemInfo = _shopItemsCategories[_selectedCategory].ShopItemsInfos[_shopItemsCategories[_selectedCategory].SelectedItem];
        _equipButton.gameObject.SetActive(currentItemInfo.HaveItem);
        _buyButton.gameObject.SetActive(!_equipButton.gameObject.activeSelf);
        //_buyButton.enabled = currentItemInfo.CanBuy;
    }

    public void SelectCategory(int categoryIndex)
    {
        AudioManager.Instance.PlayOneShotSound(_categorySwitchSound, 1, 0, 2);
        _selectedCategory = categoryIndex;

        for(int i = 0; i < _categoriesButtonsImages.Count; i++)
        {
            _categoriesButtonsImages[i].color = i == categoryIndex ? _selectedCategoryColor : _deSelectedCategoryColor;
        }

        HandleEquipAndBuyButton();
    }

    public void EquipUnequipCurrentItem()
    {
        ShopItemInfo currentItemInfo = _shopItemsCategories[_selectedCategory].ShopItemsInfos[_shopItemsCategories[_selectedCategory].SelectedItem];
        PickupItem pickupItem = currentItemInfo.PickupItem;

        AudioManager.Instance.PlayOneShotSound(_equipSound, 1, 0, 2);


        // Unequip all items from the same category, except the one we are about to handle
        foreach (ShopItemInfo shopItemInfo in _shopItemsCategories[_selectedCategory].ShopItemsInfos)
        {
            if ((AmmoPickup)shopItemInfo.PickupItem != pickupItem)
            {
                _saveManager.HandleEquipItem((AmmoPickup)shopItemInfo.PickupItem, false);
                shopItemInfo.HandleEquipSignal(false);
            }
        }

        // The second bool (true here) is ignored, we are instead giving opposite as true to equip/unequip the weapon
        bool newEquipped = _saveManager.HandleEquipItem((AmmoPickup)pickupItem, true, true);

        currentItemInfo.HandleEquipSignal(newEquipped);
    }

    public bool CheckIfEquipped(Shooter shooter)
    {
        return _saveManager.CheckIfEquipped(shooter);
    }

    #endregion

    #region Buying

    public void ModifyCash(float amount)
    {
        AudioManager.Instance.PlayOneShotSound(_cashSound, 1, 0, 2);

        SavedData savedData = _saveManager.SavedData;
        float newCash = savedData.Cash + amount;
        newCash = Mathf.Max(0, newCash);

        savedData.Cash = newCash;
        _saveManager.Save(savedData);

        _cashAmountText.text = savedData.Cash + "$";
    }

    public void Buy()
    {
        ShopItemInfo currentItemInfo = _shopItemsCategories[_selectedCategory].ShopItemsInfos[_shopItemsCategories[_selectedCategory].SelectedItem];
        if (currentItemInfo.CanBuy)
        {
            // We reduce the amount of cash we have by the item's price
            ModifyCash(-currentItemInfo.PickupItem.Price);

            // Now we get the item
            _saveManager.ObtainItem(currentItemInfo.PickupItem);

            // After buying the item, we should become able to equip it, so we activate the equip button and deactivate the buy button through this function
            HandleEquipAndBuyButton();
        } else
        {
            AudioManager.Instance.PlayOneShotSound(_notEnoughCashSound, 1, 0, 2);
        }
    }

    #endregion
    public void Quit()
    {
        SceneManager.LoadScene(0);
    }
}
