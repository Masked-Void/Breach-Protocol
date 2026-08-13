using UnityEngine;
using System.Collections.Generic;

public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;
    GameObject weaponCard;


    private Camera mainCam;
    private IPickWeapon picker;

    void Start()
    {
        mainCam = Camera.main;
        TryGetComponent(out picker);
        weaponCard = gameManager.instance.weaponStatsUI;
    }

    private void Update()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused) return;

        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.LeftControl))
            weaponManager.instance.attack();

        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<pickWeapon>(out var weaponPickup))
            {
                gameManager.instance.pickUpUI.SetActive(true);
                showWeaponStats(weaponPickup.weapon);
                setWeaponCardScale(hit.distance);
                if (Input.GetKeyDown(KeyCode.E) && picker != null)
                {
                    weaponPickup.interact(picker);
                    gameManager.instance.pickUpUI.SetActive(false);
                    gameManager.instance.weaponStatsUI.SetActive(false);
                }
                return;
            }
        }
        gameManager.instance.pickUpUI.SetActive(false);
        gameManager.instance.weaponStatsUI.SetActive(false);
    }

    void showWeaponStats(weaponStats weapon)
    {
        weaponCard.SetActive(true);
    }

    void setWeaponCardScale(float dist)
    {
        float clampedDist = Mathf.Clamp(dist, .7f, 1.3f);
        weaponCard.transform.GetChild(0).localScale = new Vector2(clampedDist, clampedDist);
    }
}