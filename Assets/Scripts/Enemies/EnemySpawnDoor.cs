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




public class EnemySpawnDoor : MonoBehaviour
{
    [SerializeField] GameObject door;
    [SerializeField] Collider doorCollider;

    float doorOpening = 3f;
    float doorClosing = 3f;
    float holdOpenTime = 1f;
    float timer = 0f;

    Vector3 doorStartPos;
    Vector3 doorOpenPos;

    bool isAnimating = false;
    bool shouldReopen = false;
    Coroutine currentCoroutine;

    private void Start()
    {
        doorStartPos = door.transform.localPosition;
        doorOpenPos = new Vector3(-1,0, 0);

    }



    private void OnTriggerEnter(Collider other)
    {

        if (isAnimating)
        {
           shouldReopen = true;

        }else
        {
            currentCoroutine = StartCoroutine(DoorOpening());
        }
        
    }




    IEnumerator DoorOpening()
    {
        isAnimating = true;
        shouldReopen = false;

        // store current position of door 
        Vector3 currentPos = door.transform.localPosition;

        timer = 0f;
        // Open
        while (timer < doorOpening)
        {
            timer += Time.deltaTime;

            float t = timer / doorOpening;

            door.transform.localPosition = Vector3.Lerp(currentPos, doorOpenPos, t);

            yield return null;

        }
        door.transform.localPosition = doorOpenPos;

        //Wait to close
        yield return new WaitForSeconds(holdOpenTime);

        //Begin closing
        timer = 0f;
        while (timer < doorClosing)
        {
            //If walks back in trigger mid close back to open
           if (shouldReopen)
            {
                isAnimating = false;
                currentCoroutine = StartCoroutine(DoorOpening());
                yield break;
            }

           //Else keep closing
            timer += Time.deltaTime;
            float t = timer / doorClosing;

            door.transform.localPosition = Vector3.Lerp(doorOpenPos,doorStartPos, t);

            yield return null;
        }

        door.transform.localPosition = doorStartPos;

        isAnimating = false;
    }



}
