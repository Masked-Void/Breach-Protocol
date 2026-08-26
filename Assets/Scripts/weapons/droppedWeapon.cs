using UnityEngine;


public class droppedWeapon : MonoBehaviour
{
    [Tooltip("Which layers count as ground. Enemies and props should be excluded.")]
    public LayerMask groundLayers = ~0;

    bool hasLanded;

    void OnCollisionEnter(Collision collision)
    {
        if (hasLanded) return;

        // Hitting an enemy shouldn't count as landing.
        if (((1 << collision.gameObject.layer) & groundLayers) == 0) return;

        hasLanded = true;
        Destroy(gameObject);
    }
}
