using UnityEngine;

public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;
    [SerializeField] KeyCode throwKey;
    [SerializeField] KeyCode switchKey;

    private Camera mainCam;
    private IPickWeapon picker;
    public bool shopOpen = false;
    private bool isShowingUI = false;

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
            }
            return;
        }

        if (gameManager.instance != null && gameManager.instance.isPaused)
        {
            setInteractionUI(false);
            return;
        }

        if (Input.GetButtonDown("Fire1") && weaponManager.instance != null)
            weaponManager.instance.attack();

        if (Input.GetKeyDown(throwKey) && weaponManager.instance != null)
            weaponManager.instance.throwWeapon();

        handleInteraction();
    }

    private void handleInteraction()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<pickWeapon>(out var weaponPickup) && weaponPickup.enabled)
            {
                gameManager.instance.interactionText.text = "Pick Up!";
                setInteractionUI(true);
                if (Input.GetButtonDown("Interact") || Input.GetKeyDown(KeyCode.E))
                {
                    weaponPickup.interact(picker);
                    Destroy(hit.collider.gameObject);
                    setInteractionUI(false);
                }
                return;
            }

            if (hit.collider.CompareTag("Shop"))
            {
                gameManager.instance.interactionText.text = "Open Shop!";
                setInteractionUI(true);
                if (Input.GetKeyDown(KeyCode.E)) openShop();

                return;
            }
        }
        setInteractionUI(false);
    }

    private void openShop()
    {
        setInteractionUI(false);
        gameManager.instance.statePause();
        shopOpen = true;
        if (gameManager.instance.shopUI != null) gameManager.instance.shopUI.SetActive(true);
    }

    private void closeShop()
    {
        gameManager.instance.stateUnpause();
        shopOpen = false;
        if (gameManager.instance.shopUI != null) gameManager.instance.shopUI.SetActive(false);
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