using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Container : MonoBehaviour
{
    [System.Serializable]
    public class ContainerItem
    {
        public System.Guid Id;
        public string Name = "";
        public int Maximum = 10;
        public int AmountTaken = 0;

        public int Remaining()
        {
            return Maximum - AmountTaken;
        }

        public int Get(int amount)
        {
            if (AmountTaken + amount > Maximum)
            {
                int toTake = Maximum - AmountTaken;
                AmountTaken = Maximum;
                return toTake;
            }

            AmountTaken += amount;
            return amount;
        }

        public int Put(int amount)
        {
            int toPut = AmountTaken - amount < 0 ? AmountTaken : amount;
            AmountTaken = Mathf.Max(0, AmountTaken - amount);

            return toPut;
        }
    }

    [SerializeField] private List<ContainerItem> _items = new List<ContainerItem>();

    public System.Guid Add(string name, int maximum)
    {
        _items.Add(new ContainerItem { Id = System.Guid.NewGuid(), Name = name, Maximum = maximum });

        return _items.Last().Id;
    }

    public void Put(string name, int amount)
    {
        ContainerItem containerItem = _items.Where((item) => item.Name == name).FirstOrDefault();
        if (containerItem != null)
        {
            containerItem.Put(amount);
        }
    }

    public int TakeFromContainer(System.Guid id, int amount)
    {
        ContainerItem containerItem = GetContainerItem(id);
        if (containerItem == null) return -1;

        return containerItem.Get(amount);
    }

    public int RemainingById(System.Guid containerItemId)
    {
        ContainerItem containerItem = GetContainerItem(containerItemId);
        if (containerItem == null) return 0;

        return containerItem.Remaining();
    }

    private ContainerItem GetContainerItem(System.Guid containerItemId)
    {
        return _items.Where(item => item.Id == containerItemId).FirstOrDefault();
    }
}
