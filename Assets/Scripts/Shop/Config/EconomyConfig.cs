using UnityEngine;

/*
 * Script: EconomyConfig
 *
 * Description:
 * How much of each currency the player earns. Bytes are spent during a run,
 * Files carry over between runs.
 *
 */

[CreateAssetMenu(menuName = "Config/EconomyConfig")]
public class EconomyConfig : ScriptableObject
{
    [Header("Files - Kept between runs")]
    [Tooltip("Files awarded for each wave completed: default is 1")]
    public int filesPerWave = 1;

    [Tooltip("Files awarded for beating the boss: default is 5")]
    public int filesForBossBeat = 5;
}
