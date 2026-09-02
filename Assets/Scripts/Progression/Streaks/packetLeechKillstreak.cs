using UnityEngine;

/// <summary>
/// PACKET LEECH: normal player kills refund ammo to the currently held weapon.
/// The weapon controller must implement IAmmoRefundReceiver.
/// </summary>
public class PacketLeechKillstreak : KillstreakBase
{
    [Header("Packet Leech")]
    [SerializeField] private int ammoRefundPerKill = 1;

    protected override void onActivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetPacketLeech(
                true,
                ammoRefundPerKill
            );
        }
    }

    protected override void onDeactivate()
    {
        if (KillstreakManager.instance != null)
        {
            KillstreakManager.instance.SetPacketLeech(
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
