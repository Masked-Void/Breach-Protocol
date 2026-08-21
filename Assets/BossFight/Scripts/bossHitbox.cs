using UnityEngine;

public class bossHitbox : MonoBehaviour, IDamage
{

    [Header("References")]
    [SerializeField] private bossFightManager fightManager;

    // Possible crit point, thought it could be cool and thought of it while dealing the boss damage
    [Header("Damage")]
    [SerializeField] private float damageMult = 1f;

    void Awake() {
        if (fightManager == null) {
            Debug.LogError("bossHitbox: no bossFightManager assigned on " + name , this);
            enabled = false;
        }
    }

    public void takeDamage(int amount) {
        Debug.Log("Damage Taken");
        if (fightManager == null) {
            return;
        }

        fightManager.takeDamage(Mathf.RoundToInt(amount*damageMult));
    }
}
