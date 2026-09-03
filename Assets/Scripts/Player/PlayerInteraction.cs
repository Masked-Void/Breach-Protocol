using UnityEngine;

/*
 * Script: PlayerInteraction
 *
 * Description:
 * Everything the player does with the world in front of them. Raycasts forward
 * each frame, shows the interact prompt when it hits something usable, and
 * routes fire and throw input to WeaponManager.
 *
 * Responsibilities:
 * - Fire and throw input
 * - Forward raycast for weapon pickups and shop terminals
 * - Show and hide the interact prompt
 * - Open and close the in-run shop, pausing the game while it's up
 *
 * Interacts With:
 * - WeaponManager (Attack and ThrowWeapon)
 * - PickWeapon (weapon pickups on the ground)
 * - GameManager (interact prompt, pause state, shop UI)
 *
 * Notes:
 * - The shop pauses the game, so this checks shopOpen before anything else and
 *   only listens for E while it's up.
 * - Interact is bound twice, to the Interact axis and to E directly. One of
 *   those is redundant.
 */
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [Tooltip("how far ahead the player can reach, in metres")]
    [Range(1f, 5)][SerializeField] private float maxDistance = 2f;

    [Tooltip("layers the interact ray can hit, keep this narrow so it doesn't catch geometry")]
    [SerializeField] LayerMask interactLayer;

    [Tooltip("key that throws the held weapon, the gdd uses throwing to swap")]
    [SerializeField] KeyCode throwKey;

    private Camera mainCam;

    // the thing that receives a picked up weapon, found on this object at Start
    private IPickWeapon picker;

    // true while the shop is up, blocks all other input
    public bool shopOpen = false;

    // tracks prompt state so we're not calling SetActive every frame
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

    // raycasts forward and shows the right prompt for whatever it hits
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
                if (Input.GetKeyDown(KeyCode.E))
                    openShop();

                return;
            }
        }
        setInteractionUI(false);
    }

    // pauses the game behind the shop panel
    private void openShop()
    {
        setInteractionUI(false);
        GameManager.instance.StatePause();
        shopOpen = true;
        if (GameManager.instance.shopUI != null)
            GameManager.instance.shopUI.SetActive(true);
    }

    // unpauses and hides the shop panel
    private void closeShop()
    {
        GameManager.instance.StateUnpause();
        shopOpen = false;
        if (GameManager.instance.shopUI != null)
            GameManager.instance.shopUI.SetActive(false);
        setInteractionUI(false);
    }

    // early outs when the state hasn't changed, SetActive every frame is wasteful
    void setInteractionUI(bool active)
    {
        if (isShowingUI == active)
            return;
        isShowingUI = active;

        if (GameManager.instance != null && GameManager.instance.interactionUI != null)
        {
            GameManager.instance.interactionUI.SetActive(active);
        }
    }
}