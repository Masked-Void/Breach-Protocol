using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;
    [SerializeField] LayerMask interactLayer;

    [SerializeField] KeyCode throwKey;

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

        if (GameManager.instance != null && GameManager.instance.isPaused)
        {
            setInteractionUI(false);
            return;
        }

        if (Input.GetButtonDown("Fire1") && WeaponManager.instance != null)
            WeaponManager.instance.Attack();

        if (Input.GetKeyDown(throwKey) && WeaponManager.instance != null)
            WeaponManager.instance.ThrowWeapon();

        handleInteraction();
    }

    private void handleInteraction()
    {
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactLayer))
        {
            if (hit.collider.TryGetComponent<PickWeapon>(out var weaponPickup) && weaponPickup.enabled)
            {
                GameManager.instance.interactionText.text = "Pick Up!";
                setInteractionUI(true);
                if (Input.GetButtonDown("Interact") || Input.GetKeyDown(KeyCode.E))
                {
                    weaponPickup.Interact(picker);
                    Destroy(hit.collider.gameObject);
                    setInteractionUI(false);
                }
                return;
            }

            if (hit.collider.CompareTag("Shop"))
            {
                GameManager.instance.interactionText.text = "Open Shop!";
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
        GameManager.instance.StatePause();
        shopOpen = true;
        if (GameManager.instance.shopUI != null) GameManager.instance.shopUI.SetActive(true);
    }

    private void closeShop()
    {
        GameManager.instance.StateUnpause();
        shopOpen = false;
        if (GameManager.instance.shopUI != null) GameManager.instance.shopUI.SetActive(false);
        setInteractionUI(false);
    }

    void setInteractionUI(bool active)
    {
        if (isShowingUI == active) return;
        isShowingUI = active;

        if (GameManager.instance != null && GameManager.instance.interactionUI != null)
        {
            GameManager.instance.interactionUI.SetActive(active);
        }
    }
}