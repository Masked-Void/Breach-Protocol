using UnityEngine;

// a damageable part of the boss. sits on colliders around the model and passes
// hits through to the fight manager, so damage can be tuned per body part.
public class BossHitbox : MonoBehaviour, IDamage
{
    [Header("References")]
    [Tooltip("the fight manager this hitbox reports to, must be assigned")]
    [SerializeField] private BossFightManager fightManager;

    [Header("Damage")]
    [Tooltip("hits here are multiplied by this, so a head or weak point can be worth more")]
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
