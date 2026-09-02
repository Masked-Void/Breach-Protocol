using UnityEngine;

public class BossHitbox : MonoBehaviour, IDamage
{

    [Header("References")]
    [SerializeField] private BossFightManager fightManager;

    // Possible crit point, thought it could be cool and thought of it while dealing the boss damage
    [Header("Damage")]
    [SerializeField] private float damageMult = 1f;

    void Awake() {
        if (fightManager == null) {
            Debug.LogError("BossHitbox: no BossFightManager assigned on " + name , this);
            enabled = false;
        }
    }

    public void TakeDamage(int amount) {

        if (fightManager == null) {
            return;
        }

        fightManager.TakeDamage(Mathf.RoundToInt(amount*damageMult));
    }
}
