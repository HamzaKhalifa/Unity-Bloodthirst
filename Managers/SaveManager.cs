using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SavedData
{
    public string Nickname = "";
    public string Email = "";
    public string Password = "";
    public bool Connected = false;

    // By default, we should have the thompson and the normal pistol in possession
    public List<int> EquippedItems = new List<int> { 10, 16 };

    public float Cash = 0;
    public List<int> BoughtItems = new List<int> { 10, 16 };
}

public class SaveManager : MonoBehaviour
{
    const string SAVED_DATA_NAME = "SavedData";

    [Header("All the pickable items. Attention! They have to be in order because we store the index in player prefs and we also use it in synchronization with the shop")]
    [SerializeField] private List<AmmoPickup> _allItems = new List<AmmoPickup>();

    public SavedData SavedData
    {
        get
        {
            SavedData savedData = JsonUtility.FromJson<SavedData>(PlayerPrefs.GetString(SAVED_DATA_NAME));
            if (savedData == null)
            {
                savedData = new SavedData();
            }

            return savedData;
        }
    }

    public bool IsConnected
    {
        get
        {
            if (SavedData != null)
                return SavedData.Connected;

            return false;
        }
    }

    public void Save(SavedData dataToSave)
    {
        PlayerPrefs.SetString(SAVED_DATA_NAME, JsonUtility.ToJson(dataToSave));
    }

    public void UpdateConnected(bool connected)
    {
        SavedData savedData = SavedData;
        savedData.Connected = connected;
        GameManager.Instance.SaveManager.Save(savedData);
    }

    #region Equipped Items

    public List<AmmoPickup> EquippedItems
    {
        get
        {
            List<AmmoPickup> equippedItems = new List<AmmoPickup>();
            for (int i = 0; i < SavedData.EquippedItems.Count; i++)
            {
                equippedItems.Add(_allItems[SavedData.EquippedItems[i]]);
            }

            return equippedItems;
        }
    }

    public bool HandleEquipItem(AmmoPickup item, bool equip, bool opposite = false)
    {
        int itemIndex = 0;
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (_allItems[i] == item)
            {
                itemIndex = i;
                break;
            }
        }

        SavedData savedData = JsonUtility.FromJson<SavedData>(PlayerPrefs.GetString(SAVED_DATA_NAME));
        if (savedData == null)
        {
            savedData = new SavedData();
        }

        bool alreadyEquipped = false;
        foreach (int equippedItem in savedData.EquippedItems)
        {
            if (equippedItem == itemIndex)
            {
                alreadyEquipped = true;
            }
        }

        // Sometimes we don't use the equip parameter, but rather use the opposite of the current value
        if (opposite)
        {
            equip = !alreadyEquipped;
        }

        if (equip && !alreadyEquipped)
        {

            savedData.EquippedItems.Add(itemIndex);
            Save(savedData);
        }

        if (!equip && alreadyEquipped)
        {
            savedData.EquippedItems.RemoveAll(index => index == itemIndex);
            Save(savedData);
        }

        return equip;
    }

    public bool CheckIfEquipped(Shooter shooter)
    {
        foreach (int i in SavedData.EquippedItems)
        {
            if (_allItems[i].Shooter == shooter)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Cash Handling

    public bool HaveItem(PickupItem item)
    {
        for (int i = 0; i < _allItems.Count; i++)
        {
            if (_allItems[i] == item)
            {
                for (int j = 0; j < SavedData.BoughtItems.Count; j++)
                {
                    if (SavedData.BoughtItems[j] == i)
                        return true;
                }

                break;
            }
        }

        return false;
    }

    public bool CanBuy(float price)
    {
        return SavedData.Cash >= price;
    }

    public void ObtainItem(PickupItem pickupItem)
    {
        int itemIndex = -1;

        for (int i = 0; i < _allItems.Count; i++)
        {
            if (_allItems[i] == pickupItem)
            {
                itemIndex = i;
            }
        }

        SavedData savedData = SavedData;
        if (!savedData.BoughtItems.Contains(itemIndex))
        {
            savedData.BoughtItems.Add(itemIndex);
        }

        Save(savedData);
    }

    #endregion
}
