using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;


public class bossFightManager : MonoBehaviour
{
    [SerializeField] public GameObject immuneBarObj;
    [SerializeField] public Image healthBar;
    [SerializeField] public Image immuneBar;

    [SerializeField] public GameObject boss;

    [SerializeField] public bossWaveManager waveManager;

    [SerializeField] int maxHealth = 100;
    int curHealth;

    [Range(0f, 1f)][SerializeField] float p1EndHealthPerc = .75f;
    [Range(0f, 1f)][SerializeField] float p2EndHealthPerc = .5f;
    [Range(0f, 1f)][SerializeField] float p3EndHealthPerc = .25f;

    [SerializeField] float p1EndImmuneTime = 10f;
    [SerializeField] float p2EndImmuneTime = 10f;
    [SerializeField] float p3EndImmuneTime = 10f;

    [SerializeField] float areaHoldTime = 10f;

    [SerializeField] bool dealDamage = false;
    [SerializeField] int damageAmt = 10;

    [System.NonSerialized] public List<bool> phaseActive = new List<bool> { false,false,false,false };
    [System.NonSerialized] public List<bool> phaseActivated = new List<bool> { false,false,false,false };
    [System.NonSerialized] public List<bool> phaseImmuned = new List<bool> { false,false,false,false };
    [System.NonSerialized] public List<float> endHealthReqs = new List<float> { 0f, 0f, 0f, 0f };
    [System.NonSerialized] public List<float> endImmuneTimes = new List<float> { 10f,10f,10f,0f };

    public bool isImmune;

    public int phase = 0;

    maskShake shake;
    void Awake()
    {
        if (boss == null)
        {
            this.enabled = false;
        }
        endHealthReqs[0] = p1EndHealthPerc * maxHealth;
        endHealthReqs[1] = p2EndHealthPerc * maxHealth;
        endHealthReqs[2] = p3EndHealthPerc * maxHealth;
        endHealthReqs[3] = 0f;

        endImmuneTimes[0] = p1EndImmuneTime;
        endImmuneTimes[1] = p2EndImmuneTime;
        endImmuneTimes[2] = p3EndImmuneTime;
        endImmuneTimes[3] = 0f;

        curHealth = maxHealth;

        Transform mask = findMark("Head");
        if (!mask.TryGetComponent(out shake))
        {
            Debug.LogError("bossFightManager: No maskShake component on boss",this);
        }
        
    }

    void Start()
    {
        phase = 0;
        phaseActive[0] = true;
        startPhase(0);
    }

    // Update is called once per frame
    void Update()
    {
        if (dealDamage)
        {
            if (!isImmune)
            {
                curHealth -= damageAmt;
                dealDamage = false;
            }
        }

        if (!isImmune && !phaseActivated[phase] && curHealth <= endHealthReqs[phase])
        {
            phaseActivated[phase] = true;
            StartCoroutine(phaseTransition(phase));
        }

        updateBossUI();
    }


    void updateBossUI()
    {

        healthBar.fillAmount = (float)curHealth / (float)maxHealth;

    }

    IEnumerator phaseTransition(int endingPhase)
    {
        phaseActive[endingPhase] = false;

        bool lastPhase = endingPhase >= phaseActive.Count - 1;

        if (!lastPhase) {

            if (endingPhase == 0) waveManager.endP1();
            if (endingPhase == 1) waveManager.endP2();
            if (endingPhase == 2) waveManager.endP3();

            isImmune = true;
            phaseImmuned[endingPhase] = true;
            immuneBarObj.SetActive(true);

            float duration = endImmuneTimes[endingPhase];
            float timer = 0f;
            while (timer < duration)
            {
                if (!shake.doShake)
                {
                    shake.doShake = true;
                }
                timer += Time.deltaTime;
                immuneBar.fillAmount = 1f - (timer / duration);
                yield return null;
            }

            shake.doShake = false;
            immuneBarObj.SetActive(false);
            isImmune = false;

            phase = endingPhase + 1;
            phaseActive[phase] = true;
            startPhase(phase);
        }
        else
        {
            // Handle the last phase completion logic here
            //Debug.Log("Boss fight completed!");
        }
    }

    void startPhase(int p)
    {
        switch (p)
        {
            case 0: phase1(); break;
            case 1: phase2(); break;
            case 2: phase3(); break;
            case 3: phase4(); break;
        }
    }

    public void phase1()
    {
        waveManager.startP1();
    }

    public void phase2()
    {
        waveManager.startP2();

    }

    public void phase3()
    {
        waveManager.startP3();
    }

    public void phase4()
    {
        waveManager.startP4();
    }
    // Looks through the bosses object's children for a marker with an exact name match
    Transform findMark(string wanted)
    {
        // Goes through each child of the boss object
        foreach (Transform child in boss.transform)
        {
            if (child.name.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return child;
            }
        }

        // Returns null if nothing matched, Awake handles that error
        return null;
    }
}
