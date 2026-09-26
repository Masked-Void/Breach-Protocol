using System;
using UnityEngine;
using UnityEngine.AI;

// Test-only. Drives a RangedEnemy directly with player input so you can feel
// its actual movement speed and firing, instead of watching its AI play out.
// Requires EnemyBase.aiDisabledForTesting ticked on in the Inspector, and
// RangedEnemy.DebugFireOnce() to exist.
public class EnemyPossessionDebug : MonoBehaviour
{
    [SerializeField] float mouseSensitivity = 2f;

    NavMeshAgent agent;
    RangedEnemy rangedEnemy;
    EnemyBase enemyBase;
    float possessedSpeed;
    float yaw;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rangedEnemy = GetComponent<RangedEnemy>();
        enemyBase = GetComponent<EnemyBase>();

        possessedSpeed = agent.speed;

        if (!enemyBase.aiDisabledForTesting)
        {
            Debug.LogWarning("EnemyPossessionDebug: tick aiDisabledForTesting on this enemy or its own AI will fight your input.", this);
        }
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = (transform.right * h + transform.forward * v).normalized * possessedSpeed * Time.deltaTime;

        agent.Move(move);

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity; ;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (Input.GetButtonDown("Fire1"))
        {
            rangedEnemy.DebugFireOnce();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && TimeManager.instance != null)
        {
            TimeManager.instance.SetTimeScale(0.1f);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && TimeManager.instance != null){
            TimeManager.instance.SetTimeScale(1f);
        }
    }
}
