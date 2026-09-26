using System;

// one upgrade's saved state. keeps the id, whether it's been bought and whether
// it's currently active together in one object so they can't fall out of sync.

[Serializable]
public class UpgradeEntry 
{
    public string id;
    public bool active;
    public bool purchased;

    public UpgradeEntry()
    {
        id = string.Empty;
        active = false;
        purchased = false;
    }

    public UpgradeEntry(string id, bool active = false, bool purchased = false)
    {
        this.id = id;
        this.active = active;
        this.purchased = purchased;
    }
}
