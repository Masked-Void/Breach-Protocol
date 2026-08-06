using System.Collections;
using UnityEngine;

using UnityEngine.UI;

public class bossFightManager : MonoBehaviour
{
    [SerializeField] public GameObject immuneBarObj;
    [SerializeField] public Image healthBar;
    [SerializeField] public Image immuneBar;

    [SerializeField] int maxHealth=100;
    int currentHealth;

    [Range(0f, 1f)] [SerializeField] float phase1HealthPerc = .75f;
    [Range(0f, 1f)] [SerializeField] float phase2HealthPerc = .5f;
    [Range(0f, 1f)] [SerializeField] float phase3HealthPerc = .25f;

    float phase1Health;
    float phase2Health;
    float phase3Health;

    bool p1Active;
    bool p1Activated;
    bool p1Immuned;

    bool p2Active;
    bool p2Activated;
    bool p2Immuned;

    bool p3Active;
    bool p3Activated;
    bool p3Immuned;

    bool p4Active;
    bool p4Activated;
    bool p4Immuned;

    bool phase1Active;
    bool phase1Activated;
    bool phase2Active;
    bool phase2Activated;
    bool phase3Active;
    bool phase3Activated;
    bool phase4Active;
    bool phase4Activated;

    [SerializeField] bool dealDamage = false;
    [SerializeField] int damageAmt = 10;

    public bool isImmune;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        phase1Health = phase1HealthPerc * maxHealth;
        phase2Health = phase2HealthPerc * maxHealth;
        phase3Health = phase3HealthPerc * maxHealth;
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (dealDamage && !isImmune)
        {
            currentHealth-=damageAmt;
            dealDamage = false;
        }

        if (currentHealth < phase1Health && !phase1Activated)
        {
            Debug.Log("Reached Phase 1");
            phase1Activated = true;
            phase1Active = true;
        }
        if (currentHealth < phase2Health && !phase2Activated)
        {
            Debug.Log("Reached Phase 2");
            phase1Active = false;
            phase2Activated = true;
            phase2Active = true;
        }
        if (currentHealth < phase3Health && !phase3Activated)
        {
            Debug.Log("Reached Phase 3");
            phase2Active = false;
            phase3Activated = true;
            phase3Active = true;
        }

        updateBossUI();
    }


    void updateBossUI()
    {

        if (phase1Activated && !p1Immuned)
        {
            // Do something for phase 1
            p1Immuned = true;
            StartCoroutine(immuneCoroutine(1));
        }
        if (phase2Activated && !p2Immuned)
        {
            // Do something for phase 2
            p2Immuned = true;
            StartCoroutine(immuneCoroutine(2));
        } if (phase3Activated && !p3Immuned)
        {
            // Do something for phase 3
            p3Immuned = true;
            StartCoroutine(immuneCoroutine(3));
        }

        immuneBar.fillAmount = (float)currentHealth / maxHealth;
        healthBar.fillAmount = immuneBar.fillAmount;

    }

    IEnumerator immuneCoroutine(int phase)
    {
        isImmune = true;
        immuneBarObj.SetActive(true);
        yield return new WaitForSecondsRealtime(5f);
        isImmune = false;
        immuneBarObj.SetActive(false);
    }
}
