using UnityEngine;

/// <summary>
/// ICE WALL: mounts a shield object on the player's back.
/// The shield remains until it absorbs its configured number of hits.
/// </summary>
public class IceWallKillstreak : KillstreakBase
{
    [Header("ICE Wall")]
    [Tooltip("shield object spawned on the player, needs an IceWallShield or one gets added")]
    [SerializeField] private GameObject shieldPrefab;

    [Tooltip("hits the shield absorbs before breaking, not damage numbers")]
    [SerializeField] private int hitsToAbsorb = 3;

    [Header("Back Mount")]
    [Tooltip("offset from the player root, roughly shoulder height and behind")]
    [SerializeField] private Vector3 localPosition = new Vector3(0f, 1.1f, -0.35f);

    [Tooltip("rotation offset, zero faces the same way as the player")]
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;

    [Tooltip("scale of the shield relative to the prefab")]
    [SerializeField] private Vector3 localScale = Vector3.one;

    private GameObject shieldInstance;

    protected override void onActivate()
    {
        if (shieldPrefab == null ||
            GameManager.instance == null ||
            GameManager.instance.player == null)
        {
            Debug.LogWarning(
                "ICE Wall requires a shield prefab and a valid player.",
                this
            );

            Deactivate();
            return;
        }

        Transform player = GameManager.instance.player.transform;

        shieldInstance = Instantiate(shieldPrefab, player);
        shieldInstance.transform.localPosition = localPosition;
        shieldInstance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        shieldInstance.transform.localScale = localScale;

        IceWallShield shield =
            shieldInstance.GetComponent<IceWallShield>();

        if (shield == null)
            shield = shieldInstance.AddComponent<IceWallShield>();

        shield.Configure(this, Mathf.Max(1, hitsToAbsorb));
    }

    public void NotifyShieldBroken()
    {
        if (isActive)
            Deactivate();
    }

    protected override void onDeactivate()
    {
        if (shieldInstance != null)
            Destroy(shieldInstance);

        shieldInstance = null;
    }

    private void Reset()
    {
        killstreakName = "ICE Wall";

        // Manual/charge-based: stays active until the shield breaks.
        duration = -1f;
    }
}

