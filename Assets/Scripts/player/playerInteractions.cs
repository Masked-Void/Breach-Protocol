using UnityEngine;

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
        if (gameManager.instance != null && gameManager.instance.isPaused) {
            isShowingUI = false;
            return;
        };

        if (Input.GetButtonDown("Fire1"))
            weaponManager.instance.attack();

        handleInteraction();
    }

    private void handleInteraction()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<pickWeapon>(out var weaponPickup) && weaponPickup.enabled)
            {
                setInteractionUI(true);
                if (Input.GetButtonDown("Interact"))
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