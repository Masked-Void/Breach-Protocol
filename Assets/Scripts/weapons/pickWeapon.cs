using UnityEngine;

public class pickWeapon : MonoBehaviour
{
    [SerializeField] weaponStats weapon;

    public void interact(IPickWeapon pic)
    {
        if (pic != null)
        {
            weapon.isFromGround = true;
            pic.equipWeapon(weapon);
        }
    }
}