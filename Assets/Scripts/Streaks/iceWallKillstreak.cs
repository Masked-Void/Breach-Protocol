using UnityEngine;

/// <summary>
/// ICE WALL: mounts a shield object on the player's back.
/// The shield remains until it absorbs its configured number of hits.
/// </summary>
public class iceWallKillstreak : killstreakBase
{
    [Header("ICE Wall")]
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private int hitsToAbsorb = 3;

    [Header("Back Mount")]
    [SerializeField] private Vector3 localPosition = new Vector3(0f, 1.1f, -0.35f);
    [SerializeField] private Vector3 localEulerAngles = Vector3.zero;
    [SerializeField] private Vector3 localScale = Vector3.one;

    private GameObject shieldInstance;

    protected override void onActivate()
    {
        if (shieldPrefab == null ||
            gameManager.instance == null ||
            gameManager.instance.player == null)
        {
            Debug.LogWarning(
                "ICE Wall requires a shield prefab and a valid player.",
                this
            );

            Deactivate();
            return;
        }

        Transform player = gameManager.instance.player.transform;

        shieldInstance = Instantiate(shieldPrefab, player);
        shieldInstance.transform.localPosition = localPosition;
        shieldInstance.transform.localRotation = Quaternion.Euler(localEulerAngles);
        shieldInstance.transform.localScale = localScale;

        iceWallShield shield =
            shieldInstance.GetComponent<iceWallShield>();

        if (shield == null)
            shield = shieldInstance.AddComponent<iceWallShield>();

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

