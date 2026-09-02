using UnityEngine;
public class IceWallShield : MonoBehaviour, IDamage
{
    [SerializeField] private int hitsRemaining;

    private IceWallKillstreak owner;

    public void Configure(IceWallKillstreak streakOwner, int hitCapacity)
    {
        owner = streakOwner;
        hitsRemaining = Mathf.Max(1, hitCapacity);
    }

    public void takeDamage(int amount)
    {
        if (hitsRemaining <= 0)
            return;

        // Capacity is measured in hits, not damage numbers.
        hitsRemaining--;

        if (hitsRemaining <= 0)
        {
            if (owner != null)
                owner.NotifyShieldBroken();
            else
                Destroy(gameObject);
        }
    }

    public int GetHitsRemaining()
    {
        return hitsRemaining;
    }
}

