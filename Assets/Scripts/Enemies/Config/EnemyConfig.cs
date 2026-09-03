using UnityEngine;

/*
 * Script: EnemyConfig
 *
 * Description:
 * Tuning for one enemy type. One asset per type, so balance passes never
 * touch code and never conflict with anyone's work.
 *
 */

[CreateAssetMenu(menuName = "Config/EnemyConfig")]
public class EnemyConfig : ScriptableObject
{
    [Header("Health")]
    [Tooltip("How much health this enemy has: default is 1")]
    [Range(1,50)] public int maxHP = 1;


    [Header("Combat")]
    [Tooltip("Starting seconds between shots ranged enemies overwrite this from their gun: default is 1.5f")]
    [Range(0.1f, 5f)] public float shotInterval = 1.5f;

    [Tooltip("How close before a melee enemy swings: default is 2f")]
    [Range(0.1f, 20f)] public float attackRange = 2f;

    [Tooltip("Damage dealt by an enemy: default is 1")]
    [Range(1, 50)] public int attackDamage = 1;

    [Tooltip("How close a ranged enemy tries to get before firing: default is 15f")]
    [Range(1f, 50f)] public float rangedAttackRange = 15f;


    [Header("Sight")]
    [Tooltip("Cone of vision in degrees: default is 120f")]
    [Range(1f, 180f)] public float fov = 120f;

    [Tooltip("How fast the enemy turns to face the player: default is 8f")]
    [Range(0.1f, 30f)] public float turnSpeed = 8f;


    [Header("Roaming")]
    [Tooltip("Seconds to wait at a roam point before moving to the next: default is 10f")]
    public float roamWaitTime = 10f;

    [Tooltip("How close counts as 'at' a roam point: default is .1f")]
    public float roamArrivalDistance = 0.1f;

    [Tooltip("Chance per check that the enemy roams instead of waiting: default is 0.1f")]
    public float roamChance = 0.1f;

    [Tooltip("Ranged enemies stay within this far of the player while roaming: default is 20f")]
    [Range(5f,50f)] public float roamRange = 20f;


    [Header("Footsteps")]
    [Tooltip("Seconds between footstep sounds: default is 0.5f")]
    public float stepInterval = 0.5f;

    [Tooltip("Speed below this counts as standing still: default is 0.1f")]
    public float stepSpeedThreshold = 0.1f;


    [Header("Rewards")]
    [Tooltip("Bytes dropped on death before any streak multiplier: default is 5")]
    public int byteValue = 5;
}
