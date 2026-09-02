using UnityEngine;

/// <summary>
/// PACKET LEECH: normal player kills refund ammo to the currently held weapon.
/// The weapon controller must implement IAmmoRefundReceiver.
/// </summary>
public class packetLeechKillstreak : killstreakBase
{
    [Header("Packet Leech")]
    [SerializeField] private int ammoRefundPerKill = 1;

    protected override void onActivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetPacketLeech(
                true,
                ammoRefundPerKill
            );
        }
    }

    protected override void onDeactivate()
    {
        if (killstreakManager.instance != null)
        {
            killstreakManager.instance.SetPacketLeech(
                false,
                ammoRefundPerKill
            );
        }
    }

    private void Reset()
    {
        killstreakName = "Packet Leech";
        duration = 12f;
    }
}
