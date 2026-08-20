using UnityEngine;

public abstract class weaponStats : ScriptableObject
{
    [Header("Identity")]
    public string Name;

    [Header("Model")]
    public GameObject weaponModel;
    public Sprite sprite;

    [Header("Model")]
    public int cost;

    [Header("Damage")]
    [Range(.1f, 5)][SerializeField] public float attackRate;

    public abstract void Attack();
}