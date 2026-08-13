using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class playerController : MonoBehaviour, IDamage, IPickWeapon
{
    [Header("Controller")]
    [SerializeField] CharacterController controller;

    [Header("Player Settings")]
    [SerializeField] int HP;
    [SerializeField] int speed;
    [SerializeField] int sprintMod;
    [SerializeField] int jumpSpeed;
    [SerializeField] int jumpMax;
    [SerializeField] int gravity;
    [SerializeField] float pushbackFriction = 5f;
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
    //int HPOriginal;

    Vector3 moveDir;
    Vector3 playerVel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //HPOriginal = HP;
        currentStamina = maxStamina;
    }

    // Update is called once per frame
    void Update()
    {
        movement();
        updatePlayerUI();
    }
    public void PushBack(Vector3 direction, float pushbackForce)
    {
        playerVel += direction * pushbackForce;
    }
    void movement()
    {
        if (gameManager.instance != null && gameManager.instance.isPaused)
        {
            gameManager.instance.weaponStatsUI.SetActive(false);
            gameManager.instance.pickUpUI.SetActive(false);
            return;
        }

        /*if (killChainManager.instance != null && killChainManager.instance.activatePlayershield)
        {
            killChainManager.instance.activatePlayershield = false;
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
            if (stepTimer <= 0f)
            {
                audioManager.instance.playSteps();
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
            audioManager.instance.playJump();
            playerVel.y = jumpSpeed;
            jumpCount++;
        }
    }

    public void takeDamage(int amount)
    {
        audioManager.instance.playHurt();

        StartCoroutine(flashDamage());

        if (heartbeatManager.instance != null)
        {
            heartbeatManager.instance.playerDamaged();
        }
    }

    public void weaponStats(weaponStats weapon)
    {
        if (weaponManager.instance != null)
        {
            audioManager.instance.playEquip();
            weaponManager.instance.equipWeapon(weapon);
        }
    }

    public float getSpeedPercent()
    {
        // Returns a value between 0 and 1 representing how fast the player is moving relative to their max speed.
        Vector3 hor = new Vector3(moveDir.x, 0, moveDir.z);
        float horPercent = Mathf.Clamp01(hor.magnitude);

        // If the player is in the air, we also consider their vertical speed relative to jump speed.
        float vertPercent = 0;
        if (!controller.isGrounded)
        {
            vertPercent = Mathf.Clamp01(Mathf.Abs(playerVel.y) / jumpSpeed);
        }

        // Return the greater of the two percentages to represent overall movement intensity.
        return Mathf.Max(horPercent, vertPercent);
    }

    IEnumerator flashDamage()
    {
        gameManager.instance.damageFlashUI.SetActive(true);
        yield return new WaitForSeconds(.1f);
        gameManager.instance.damageFlashUI.SetActive(false);
    }

    IEnumerator addPlayerShield()
    {
        playerShield.SetActive(true);
        yield return new WaitForSeconds(10f);
        playerShield.SetActive(false);
        //killChainManager.instance.activatePlayershield = false;
    }

    public void updatePlayerUI()
    {
        gameManager.instance.playerStaminaBar.fillAmount = (float)currentStamina / maxStamina;

    }

    public void spawnPlayer()
    {
        controller.transform.position = gameManager.instance.playerSpawnPos.transform.position;
        Physics.SyncTransforms();
        currentStamina = maxStamina;
        updatePlayerUI();
    }
}