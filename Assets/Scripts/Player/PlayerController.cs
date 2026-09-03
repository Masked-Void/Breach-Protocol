using System.Collections;
using UnityEngine;

/*
 * Script: PlayerController
 *
 * Description:
 * Player movement, look, stamina and the damage entry point. Lives on the
 * Player prefab in Bootstrap, so it persists across level loads.
 *
 * Responsibilities:
 * - Movement, sprint, double jump and gravity through CharacterController
 * - Stamina drain while sprinting and regen while not
 * - Report speed to TimeManager so world time scales with the player
 * - Take damage and route it to the heartbeat system
 * - Hold the weapon mount point WeaponManager attaches to
 * - Throw the held weapon
 *
 * Interacts With:
 * - TimeManager (SpeedPercent drives world time scale)
 * - HeartbeatManager (damage adds stress)
 * - WeaponManager (weaponHoldPos, EquipWeapon via IPickWeapon)
 * - GameManager (found by tag, exposed as playerScript, drives stamina bar)
 * - LevelLoader (teleports the player to each level's spawn marker)
 *
 * Notes:
 * - Movement uses unscaled delta time on purpose. Scaled time is for the world,
 *   not the player. That inversion is the whole SUPERHOT mechanic.
 * - LevelLoader disables the CharacterController before moving the transform,
 *   because the controller overrides direct transform writes.
 */
public class PlayerController : MonoBehaviour, IPickWeapon, IDamage
{
    [Header("Controller")]
    [Tooltip("on this same object, does the actual moving and collision")]
    [SerializeField] CharacterController controller;

    [Header("Movement")]
    [Tooltip("base walk speed in units per second")]
    [SerializeField] int speed;

    [Tooltip("speed is multiplied by this while sprinting")]
    [SerializeField] int sprintMod;

    [Tooltip("upward force per jump")]
    [SerializeField] int jumpSpeed;

    [Tooltip("how many jumps before touching the ground, 2 is a double jump")]
    [SerializeField] int jumpMax;

    [Tooltip("downward acceleration, higher falls faster")]
    [SerializeField] int gravity;

    [Tooltip("how fast knockback bleeds off, higher stops the player sooner")]
    [SerializeField] float pushbackFriction = 5f;

    [Header("Throwing")]
    [Tooltip("forward force when throwing the held weapon")]
    public float throwForce = 5f;

    [Tooltip("upward arc added to the throw so it doesn't go flat")]
    public float throwUpwardForce = 5f;

    [Header("Refs")]
    [Tooltip("shield object mounted on the back by the ice wall scorestreak")]
    [SerializeField] GameObject playerShield;

    [Tooltip("empty transform the equipped weapon parents to")]
    public GameObject weaponHoldPos;

    [Header("Stamina")]
    [Tooltip("seconds of sprint from full")]
    [SerializeField] float maxStamina = 5f;

    [Tooltip("stamina used per second while sprinting")]
    [SerializeField] float staminaDrainRate = 1f;

    [Tooltip("stamina recovered per second while not sprinting")]
    [SerializeField] float staminaRegenRate = 1f;

    [Tooltip("stops sprinting sideways or backwards")]
    [SerializeField] bool sprintForwardOnly = true;

    [Header("Footsteps")]
    [Tooltip("seconds between step sounds while moving on the ground")]
    [SerializeField] float stepInterval = 0.4f;

    // runtime state
    float currentStamina;
    float stepTimer;
    int jumpCount;
    Vector3 moveDir;
    Vector3 playerVel;

    void Start()
    {
        currentStamina = maxStamina;
        ;
    }

    void Update()
    {
        movement();
        UpdatePlayerUI();
    }
    public void PushBack(Vector3 direction, float pushbackForce)
    {
        float maxPushbackForce = 8f;
        playerVel += direction * pushbackForce;
        //clamp a limit
        playerVel = Vector3.ClampMagnitude(playerVel, maxPushbackForce);
    }
    void movement()
    {
        if (GameManager.instance != null && GameManager.instance.isPaused)
        {
            GameManager.instance.interactionUI.SetActive(false);
            return;
        }

        /*if (KillChainManager.instance != null && KillChainManager.instance.activatePlayershield)
        {
            KillChainManager.instance.activatePlayershield = false;
            StartCoroutine(addPlayerShield());
        }*/

        if (controller.isGrounded)
        {
            playerVel.y = 0;
            jumpCount = 0;
        }

        float hInput = Input.GetAxisRaw("Horizontal");
        float vInput = Input.GetAxisRaw("Vertical");

        moveDir = (hInput * transform.right + vInput * transform.forward).normalized;

        bool isMoving = moveDir.sqrMagnitude > 0.01f;
        bool isMovingForward = vInput > 0;

        bool canSprint = isMoving && currentStamina > 0.01f;
        if (sprintForwardOnly)
            canSprint &= isMovingForward;

        bool isSprinting = Input.GetButton("Sprint") && canSprint;

        if (isSprinting)
        {
            currentStamina -= staminaDrainRate * Time.unscaledDeltaTime;
            if (currentStamina <= 0.01f)
            {
                currentStamina = 0.01f;
                isSprinting = false;
            }
        }
        else
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.unscaledDeltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        int currSpeed = isSprinting ? speed * sprintMod : speed;

        playerVel.x = Mathf.MoveTowards(playerVel.x, 0, pushbackFriction * Time.unscaledDeltaTime);
        playerVel.z = Mathf.MoveTowards(playerVel.z, 0, pushbackFriction * Time.unscaledDeltaTime);
        playerVel.y -= gravity * Time.unscaledDeltaTime;

        jump();

        Vector3 finalVelocity = (moveDir * currSpeed) + playerVel;
        controller.Move(finalVelocity * Time.unscaledDeltaTime);

        if (controller.isGrounded && isMoving)
        {
            stepTimer -= Time.unscaledDeltaTime;
            if (stepTimer <= 0f && AudioManager.instance != null)
            {
                AudioManager.instance.PlaySteps();
                stepTimer = isSprinting ? (stepInterval / sprintMod) : stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void jump()
    {
        if (Input.GetButtonDown("Jump") && jumpCount < jumpMax)
        {
            AudioManager.instance.PlayJump();
            playerVel.y = jumpSpeed;
            jumpCount++;
        }
    }

    public void TakeDamage(int amount)
    {
        AudioManager.instance.PlayHurt();

        StartCoroutine(flashDamage());

        if (HeartbeatManager.instance != null)
        {
            HeartbeatManager.instance.PlayerDamaged();
        }
    }

    public void EquipWeapon(WeaponStats weapon, int ammoOverride = -1)
    {
        if (WeaponManager.instance != null)
            WeaponManager.instance.EquipWeapon(weapon, ammoOverride);
    }
    public float SpeedPercent
    {
        get
        {
            // how fast we are moving sideways compared to max speed
            Vector3 hor = new Vector3(moveDir.x, 0, moveDir.z);
            float horPercent = Mathf.Clamp01(hor.magnitude);

            // in the air the fall or jump speed counts too
            float vertPercent = 0;
            if (!controller.isGrounded)
                vertPercent = Mathf.Clamp01(Mathf.Abs(playerVel.y) / jumpSpeed);

            // whichever is bigger is how fast we read as moving
            return Mathf.Max(horPercent, vertPercent);
        }
    }

    IEnumerator flashDamage()
    {
        GameManager.instance.damageFlashUI.SetActive(true);
        yield return new WaitForSecondsRealtime(.1f);
        GameManager.instance.damageFlashUI.SetActive(false);
    }

    IEnumerator addPlayerShield()
    {
        playerShield.SetActive(true);
        yield return new WaitForSeconds(10f);
        playerShield.SetActive(false);
        //KillChainManager.instance.activatePlayershield = false;
    }

    public void UpdatePlayerUI()
    {
        GameManager.instance.playerStaminaBar.fillAmount = (float)currentStamina / maxStamina;

    }

}