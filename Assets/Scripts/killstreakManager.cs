using UnityEngine;

public class killstreakManager : MonoBehaviour
{

    public static killstreakManager instance;

    [Header("Chance Settings")]
    [UnityEngine.Range(0f, 1f)]
    [SerializeField] float killstreakChance = .5f;

    [Header("Killstreak Pool")]
    [SerializeField] killstreakBase[] killstreaks;

    killstreakBase activeStreak;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }

    // Start removed — no initialization required here

    public void tryRoll()
    {
        if (killstreaks == null || killstreaks.Length == 0)
        {
            // Debug.LogWarning("killstreakManager: no killstreaks assigned"); 
            return;
        }

        if (activeStreak != null && activeStreak.isActive)
        {
            // Debug.Log("Roll skipped"); 
            return;
        }

        if (Random.value > killstreakChance)
        {
            // Debug.Log("Roll Failed"); 
            return;
        }

        killstreakBase picked = killstreaks[Random.Range(0, killstreaks.Length)];
        activeStreak = picked;
        picked.Activate();
    }

    public void streakEnded(killstreakBase streak)
    {
        if(activeStreak == streak)
        {
            activeStreak = null;
        }
    }

    public void cancelActiveStreak()
    {
        if (activeStreak != null && activeStreak.isActive)
        {
            activeStreak.Deactivate();
        }
    }
}
