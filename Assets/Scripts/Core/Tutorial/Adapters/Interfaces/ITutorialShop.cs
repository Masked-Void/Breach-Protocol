using System;

// what the tutorial needs from the shop popup and the Byte wallet
public interface ITutorialShop
{
    // raised when the shop popup closes
    event Action Closed;

    // raised after a purchase, passes the item id
    event Action<string> Purchased;

    // points the shop popup at the tutorial stock
    void SetStock(TutorialShopConfig stock);

    // opens the popup, the way it opens between rounds
    void OpenShop();

    // gives Bytes straight to the player, the way a cleared round pays
    void GrantBytes(int amount);

    // zeroes tutorial Bytes and puts the real shop popup back
    void ClearTutorialState();
}
