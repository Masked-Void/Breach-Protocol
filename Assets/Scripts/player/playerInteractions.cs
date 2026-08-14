using UnityEngine;


public class playerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;
    [SerializeField] GameObject shopUI;
    
    


    private Camera mainCam;
    private IPickWeapon picker;
    public bool shopOpen = false;
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
        if (shopOpen)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                closeShop();
                return;
            }
        }
        

        if (gameManager.instance != null && gameManager.instance.isPaused && !shopOpen) return;

        if (gameManager.instance != null && gameManager.instance.isPaused) {
            isShowingUI = false;
            return;
        };

        if (Input.GetButtonDown("Fire1"))
            weaponManager.instance.attack();

        if (Input.GetKeyDown(KeyCode.Keypad8))
            weaponManager.instance.Throw();

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
                if (Input.GetButtonDown("Interact") && picker != null)
                setInteractionUI(true);
                if (Input.GetButtonDown("Interact"))
                {
                    weaponPickup.interact(picker);
                    Destroy(hit.collider.gameObject);
                    setInteractionUI(false);
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
        setInteractionUI(false);

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