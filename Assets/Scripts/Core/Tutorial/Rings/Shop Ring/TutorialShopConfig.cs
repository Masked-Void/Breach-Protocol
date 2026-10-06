using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Script: TutorialShopConfig
 *
 * Description:
 * The tutorial shop's stock and Byte grant, one asset in Tunables. Priced so the
 * grant buys exactly one thing, since the lesson is buy one, then leave.
 *
 * Interacts With:
 * - ShopRingLesson (reads the grant, hands the stock to the shop adapter)
 * - ShopAdapter (fills the tutorial panel's cards, prices purchases)
 *
 * Notes:
 * - Tutorial purchases take Bytes and nothing else. They never reach a real run.
 * - Each item fills one card the same way an UpgradeData fills a ShopPopulator
 *   card: name, description, icon and price.
 */
[CreateAssetMenu(menuName = "Config/TutorialShopConfig")]
public class TutorialShopConfig : ScriptableObject
{
    // One line in the shop
    [Serializable]
    public class Item
    {
        [Tooltip("Unique key, the shop adapter reports it when this is bought")]
        [SerializeField] private string itemId;

        [Tooltip("Name on the shop card")]
        [SerializeField] private string displayName;

        [Tooltip("One line explaining what it does")]
        [SerializeField] private string description;

        [Tooltip("Icon, optional")]
        [SerializeField] private Sprite icon;

        [Tooltip("Price in bytes")]
        [SerializeField] private int priceBytes;

        [Tooltip("Shows grayed out with locked messagee instead of price")]
        [SerializeField] private bool isLocked;

        [Tooltip("Messaged on locked item")]
        [SerializeField] private string lockedMessage = "Locked, Unlock through 'insert method'";

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int PriceBytes => priceBytes;
        public bool IsLocked => isLocked;
        public string LockedMessage => lockedMessage;
    }

    [Header("Bytes")]
    [Tooltip("Bytes paid when previous ring is cleared. Match the cheapest item")]
    [SerializeField] private int byteGrant = 100;

    [Header("Items")]
    [Tooltip("What shows in the shop, in order, one per shop slot")]
    [SerializeField] private Item[] items = new Item[0];

    public int ByteGrant => byteGrant;
    public IReadOnlyList<Item> Items => items;

    public Item FindItem(string itemId)
    {
        foreach (Item item in items)
        {
            if (item!=null && item.ItemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    // true if the grant buys one unlocked item and can never buy two, counting the same item twice
    public bool IsAffordableOnce()
    {
        int cheapest = int.MaxValue;

        foreach (Item item in items)
        {
            if (item != null && !item.IsLocked && item.PriceBytes < cheapest)
            {
                cheapest = item.PriceBytes;
            }
        }

        return cheapest <= byteGrant && cheapest * 2L > byteGrant; 
    }

    // catches a bad price in the inspector before a playtest does
    private void OnValidate()
    {
        if (items != null && items.Length > 0 && !IsAffordableOnce())
        {
            Debug.LogWarning("TutorialShopConfig: the Byte grant should buy exactly one unlocked item", this);
        }
    }
}
