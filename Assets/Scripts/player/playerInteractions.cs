using UnityEngine;
using System.Collections.Generic;

public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;


    private Camera mainCam;
    private IPickWeapon picker;
    private MonoBehaviour pickerMono;
    private bool isShowingUI = false;

    void Start()
    {
        mainCam = Camera.main;
        if (TryGetComponent(out picker))
        {
            pickerMono = picker as MonoBehaviour;
        }
    }

    private void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;

        if (Input.GetButtonDown("Fire1"))
            weaponManager.instance.attack();

        handleInteraction();
    }

    private void handleInteraction()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<pickWeapon>(out var weaponPickup))
            {
                setInteractionUI(true);

                bool isPickerActive = pickerMono == null || pickerMono.enabled;

                if (Input.GetButtonDown("Interact") && picker != null && isPickerActive)
                {
                    weaponPickup.interact(picker);
                    setInteractionUI(false);
                }
                return;
            }
        }
        setInteractionUI(false);
    }

    void setInteractionUI(bool active)
    {
        if (isShowingUI == active) return;

        isShowingUI = active;

        if (gameManager.instance != null && gameManager.instance.interactionUI != null)
        {
            gameManager.instance.interactionUI.SetActive(active);
        }
    }
}