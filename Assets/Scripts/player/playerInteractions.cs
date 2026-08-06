using UnityEngine;
using System.Collections.Generic;

public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;


    private Camera mainCam;
    private IPickWeapon picker;

    void Start()
    {
        mainCam = Camera.main;
        TryGetComponent(out picker);
    }

    private void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;

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
        }
        gameManager.instance.pickUpUI.SetActive(false);
    }
}