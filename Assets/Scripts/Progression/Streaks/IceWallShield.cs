using UnityEngine;

// the shield the Ice Wall scorestreak mounts on the player's back. soaks a set
// number of hits rather than an amount of damage, so one big hit costs the same
// as one small one.
public class IceWallShield : MonoBehaviour, IDamage
{
    [Tooltip("hits left before it breaks, set by the streak not the inspector")]
    [SerializeField] private int hitsRemaining;


    // the streak that spawned this, told when the shield breaks so it can end
    private IceWallKillstreak owner;

    // called by the streak right after spawning, since the prefab has no idea
    // how many hits it should take
    public void Configure(IceWallKillstreak streakOwner, int hitCapacity)
    {
        owner = streakOwner;
        hitsRemaining = Mathf.Max(1, hitCapacity);
    }

    public void TakeDamage(int amount)
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

    public int HitsRemaining => hitsRemaining;
}

