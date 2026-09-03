// implemented by whatever holds ammo, so Packet Leech can refund on kill
// without knowing what weapon is equipped
public interface IAmmoRefundReceiver
{
    void RefundAmmo(int amount);
}