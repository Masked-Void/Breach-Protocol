using UnityEngine;

public class pickWeapon : MonoBehaviour
{
    [SerializeField] public weaponStats weapon;

    public void interact(IPickWeapon pic)
    {
        if (pic != null)
        {
            pic.weaponStats(weapon);
            Destroy(gameObject);
        }
    }
}