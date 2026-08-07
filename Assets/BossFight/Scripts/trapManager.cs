using System;
using UnityEngine;

public class trapManager : MonoBehaviour
{

    [SerializeField] laserArrayManager laserManager;
    [SerializeField] lavaManager lavaManager;
    [SerializeField] platformManager platManager;

    [SerializeField] public bool laserActive = false;
    [SerializeField] public bool lavaActive = false;
    [SerializeField] public bool platActive = false;

    [SerializeField] public int laserCount = 0;
    [SerializeField] public int laserPillarCount = 0;
    [SerializeField] public int laserRiseFallRate = 0;
    [SerializeField] public int laserSpinRate = 0;

    [SerializeField] public int lavaRiseRate = 0;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActivateLasers(int phase)
    {
        
    }

    public void ActivateLava(int phase)
    {

    }

    public void ActivatePlatforms(int phase)
    {

    }
}
