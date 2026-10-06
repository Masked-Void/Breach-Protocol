using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * Script: ShopAdapter
 *
 * Description:
 * Connects the tutorial to a stand-in for the between-wave shop. It opens, closes
 * and pauses the same way ShopManager does, so ring 3 teaches the real controls:
 * the world freezes with no pause menu, Q closes it, and the pause menu hides it.
 *
 * Responsibilities:
 * - Fill the tutorial panel's cards from TutorialShopConfig
 * - Freeze the game while it's open and fire Closed when Q or the close button shuts it
 * - Hide behind the pause menu and come back frozen when it closes
 * - Sell for Bytes through GameManager, with its not enough Bytes warning
 *
 * Interacts With:
 * - GameManager (totalBytes, AddBytes, SubtractBytes, ShowShopWarning, FreezeGame, UnfreezeGame, isPaused)
 * - ShopRingLesson (through ITutorialShop)
 *
 * Notes:
 * - It doesn't go through ShopManager. ShopManager only opens from WaveManager.WaveCleared,
 *   only offers upgrades the save already has active, and its purchases add UpgradeManager
 *   tiers that nothing clears between runs, so a tutorial buy would carry into the first run.
 * - Uses its own panel and never touches GameManager.shopUI, so the real shop is untouched.
 */

public class ShopAdapter : MonoBehaviour ,ITutorialShop
{
    // one card on the tutorial shop panel, the same layout as a ShopPopulator card
    [Serializable]
    public class Slot
    {
        [Tooltip("the card itself, hidden when there are more cards than items")]
        [SerializeField] private GameObject root;

        [Tooltip("item name")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("one line explaining what the item does, optional")]
        [SerializeField] private TMP_Text descriptionText;

        [Tooltip("price, or the locked message")]
        [SerializeField] private TMP_Text priceText;

        [Tooltip("item icon, optional")]
        [SerializeField] private Image icon;

        [Tooltip("buys this card's item, wired from code so leave its OnClick list empty")]
        [SerializeField] private Button buyButton;

        public GameObject Root => root;

        public TMP_Text NameText => nameText;

        public TMP_Text DescriptionText => descriptionText;

        public TMP_Text PriceText => priceText;

        public Image Icon => icon;

        public Button BuyButton => buyButton;
    }

    [Header("Tutorial Shop Panel")]
    [Tooltip("the tutorial's shop popup, a copy of the real shop panel, hidden until the shop opens")]
    [SerializeField] private GameObject tutorialShopPanel;

    [Tooltip("one card per stock item, in order")]
    [SerializeField] private Slot[] slots = new Slot[0];

    [Tooltip("current Bytes on the panel, optional")]
    [SerializeField] private TMP_Text bytesText;

    [Header("Controls")]
    [Tooltip("closes the shop, keep it matching ShopManager's close key")]
    [SerializeField] private KeyCode closeKey = KeyCode.Q;

    private TutorialShopConfig stock;
    private bool isOpen;

    // true while the pause menu is covering the open shop
    private bool isSuspended;

    public event Action Closed;

    public event Action<string> Purchased;

    private void Start()
    {
        showPanel(false);
    }

    private void Update()
    {
        if (!isOpen || GameManager.instance == null)
        {
            return;
        }

        if (isSuspended)
        {
            if (!GameManager.instance.isPaused)
            {
                isSuspended = false;
                GameManager.instance.FreezeGame();
                showPanel(true);
            }
            return;
        }

        if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            isSuspended = true;
            showPanel(false);
            return;
        }

        if (Input.GetKeyDown(closeKey))
        {
            CloseShop();
        }
    }
    private void OnDestroy()
    {
        if (isOpen && !isSuspended  && GameManager.instance != null)
        {
            GameManager.instance.UnfreezeGame();
        }
    }

    public void SetStock(TutorialShopConfig newStock)
    {
        stock = newStock;
        refreshSlots();
    }

    public void OpenShop()
    {
        if (isOpen || GameManager.instance == null)
        {
            return;
        }

        GameManager.instance.FreezeGame();
        isOpen = true;
        isSuspended = false;
        refreshSlots();
        showPanel(true);
    }

    public void GrantBytes(int amount)
    {
        if (GameManager.instance != null && amount > 0)
        {
            GameManager.instance.AddBytes(amount);
        }

        refreshBytes();
    }

    public void ClearTutorialState()
    {
        if (GameManager.instance != null && GameManager.instance.totalBytes != 0)
        {
            GameManager.instance.SubtractBytes(GameManager.instance.totalBytes);
        }

        if (isOpen)
        {
            CloseShop();
        }

        stock = null;
       
    }

    public void Buy(string itemId)
    {
        TutorialShopConfig.Item item = stock!=null ? stock.FindItem(itemId) : null;

        if (!isOpen || isSuspended || item == null || item.IsLocked || GameManager.instance == null)
        {
            return;
        }

        if (GameManager.instance.totalBytes < item.PriceBytes)
        {
            GameManager.instance.ShowShopWarning();
            return;
        }

        GameManager.instance.SubtractBytes((int)item.PriceBytes);
        if(AudioManager.instance != null)
        {
            AudioManager.instance.PlayButtonClick();
        }

        refreshBytes();
        Purchased?.Invoke(itemId);
    }

    public void CloseShop()
    {
        if (!isOpen)
        {
            return;
        }

        showPanel(false);
        isOpen = false;

        if (!isSuspended && GameManager.instance != null)
        {
            GameManager.instance.UnfreezeGame();
        }

        isSuspended = false;
        Closed?.Invoke();
    }

    private void showPanel(bool isVisible)
    {
        if (tutorialShopPanel != null)
        {
            tutorialShopPanel.SetActive(isVisible);
        }
    }

    private void refreshSlots()
    {
        refreshBytes();

        int itemCount = stock!=null ? stock.Items.Count : 0;
        for (int i = 0;i<slots.Length;i++)
        {
            Slot slot = slots[i];
            bool hasItem = slot != null && i < itemCount && stock.Items[i] != null;

            if (slot != null && slot.Root != null)
            {
                slot.Root.SetActive(hasItem);
            }

            if (hasItem)
            {
                fillSlot(slot,stock.Items[i]);
            }
        }
    }

    private void fillSlot(Slot slot, TutorialShopConfig.Item item)
    {
        if (slot.NameText != null)
        {
            slot.NameText.text = item.DisplayName;
        }

        if (slot.DescriptionText != null)
        {
            slot.DescriptionText.text = item.Description;
        }

        if (slot.PriceText != null)
        {
            slot.PriceText.text = item.IsLocked ? item.LockedMessage : "Bytes " + item.PriceBytes;
        }

        if (slot.Icon != null)
        {
            slot.Icon.sprite = item.Icon;
            slot.Icon.enabled = item.Icon != null;
        }

        if (slot.BuyButton != null)
        {
            slot.BuyButton.interactable = !item.IsLocked;
            slot.BuyButton.onClick.RemoveAllListeners();

            string itemId = item.ItemId;
            slot.BuyButton.onClick.AddListener(() => Buy(itemId));
        }
    }

    private void refreshBytes()
    {
        if (bytesText != null && GameManager.instance != null)
        {
            bytesText.text = "Bytes: " + GameManager.instance.totalBytes;
        }
    }
}
