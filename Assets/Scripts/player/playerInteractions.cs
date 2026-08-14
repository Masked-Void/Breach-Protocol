using UnityEngine;
using System.Collections.Generic;


public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;
    [SerializeField] GameObject shopUI;
    
    


    private Camera mainCam;
    private IPickWeapon picker;
    public bool shopOpen = false;

    void Start()
    {
        mainCam = Camera.main;
        TryGetComponent(out picker);
    }

    private void Update()
    {
        if (shopOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                closeShop();
                return;
            }
        }
        

        if (gameManager.instance != null && gameManager.instance.isPaused && !shopOpen) return;

        if (Input.GetButtonDown("Fire1"))
            weaponManager.instance.attack();

        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<pickWeapon>(out var weaponPickup))
            {
                gameManager.instance.pickUpUI.SetActive(true);
                if (Input.GetButtonDown("Interact") && picker != null)
                {
                    weaponPickup.interact(picker);
                    gameManager.instance.pickUpUI.SetActive(false);
                }
                return;
            }

            if (hit.collider.CompareTag("Shop"))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    openShop();
                }
            }
        }
        gameManager.instance.pickUpUI.SetActive(false);

    }

    private void openShop()
    {
        
        gameManager.instance.statePause();
        
        shopOpen = true;
        shopUI.SetActive(true);
        
        
    }

    private void closeShop()
    {
        
        gameManager.instance.stateUnpause();
        shopOpen = false;
        
        
        
    }
}