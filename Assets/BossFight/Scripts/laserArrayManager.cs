using System;
using UnityEngine;

public class laserArrayManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Laser Array Settings")]
   
    [SerializeField] GameObject[] pillar1Lasers;
    [SerializeField] GameObject[] pillar2Lasers;
    [SerializeField] GameObject[] pillar3Lasers;
    [SerializeField] GameObject[] pillar4Lasers;

    
    public void ActivateLasers(int laserCount, int pillarCount, int laserRiseFallRate, int laserSpinRate)
    {
        
    }

}
