using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour, IPickWeapon, IDamage
{
    [Header("Controller")]
    [SerializeField] CharacterController controller;

    [Header("Player Settings")]
    [SerializeField] int speed;
    [SerializeField] int sprintMod;
    [SerializeField] int jumpSpeed;
    [SerializeField] int jumpMax;
    [SerializeField] int gravity;
    [SerializeField] float pushbackFriction = 5f;
    public float throwForce = 5f;
    public float throwUpwardForce = 5f;
    [SerializeField] GameObject playerShield;

    [Header("Stamina Settings")]
    [SerializeField] float maxStamina = 5f;
    [SerializeField] float staminaDrainRate = 1f;
    [SerializeField] float staminaRegenRate = 1f;
    [SerializeField] bool sprintForwardOnly = true;

    float currentStamina;

    [Header("Footstep Settings")]
    [SerializeField] float stepInterval = 0.4f;
    float stepTimer;

    int jumpCount;

    Vector3 moveDir;
    Vector3 playerVel;
    public GameObject weaponHoldPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentStamina = maxStamina;;
    }

    // Update is called once per frame
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
        if (sprintForwardOnly) canSprint &= isMovingForward;

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
            WeaponManager.instance.equipWeapon(weapon, ammoOverride);
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

    public void SpawnPlayer()
    {
        controller.transform.position = GameManager.instance.playerSpawnPos.transform.position;
        Physics.SyncTransforms();
        currentStamina = maxStamina;
        UpdatePlayerUI();
    }
}