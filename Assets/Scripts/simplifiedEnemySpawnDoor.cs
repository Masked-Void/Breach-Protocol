/*
 * This is a simplified door for enemy spawn rooms
 * 
 * utilizes layers and triggers to work.
 * 
 * 
 * created by Mark Fittante
 * 
 * 
 */

using System.Collections;
using UnityEngine;




public class simplifiedEnemySpawnDoor : MonoBehaviour
{
    [SerializeField] GameObject door;
    [SerializeField] Collider doorCollider;



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            StartCoroutine(DoorOpening());
        }
    }




    IEnumerator DoorOpening()
    {
        door.transform.position = new Vector3(-1, 0,0);
    }



}
