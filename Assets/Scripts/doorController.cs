using System.Collections;
using UnityEngine;

public class doorController : MonoBehaviour
{

    [SerializeField] float movementTime = 1;

    [SerializeField] GameObject endPosObj;
    [SerializeField] GameObject doorObj;

    Vector3 startPos;
    Vector3 endPos;

    public bool runOpen = false;
    public bool runClose = false;

    Coroutine doorRoutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        startPos = doorObj.transform.position;
        endPos = endPosObj.transform.position;
    }

    void Update()
    {
        if (runOpen)
        {
            startDoor(endPos);
            runOpen = false;
        }

        if (runClose)
        {
            startDoor(startPos);
            runClose = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out EnemyBase eb) && !eb.hasLeftSpawnRoom)
        {
            startDoor(endPos);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy") && other.TryGetComponent(out EnemyBase eb) && !eb.hasLeftSpawnRoom)
        {
            startDoor(startPos);
            eb.hasLeftSpawnRoom = true;
        }
    }

    void startDoor(Vector3 target)
    {
        if (doorRoutine != null) StopCoroutine(doorRoutine);
        doorRoutine = StartCoroutine(moveDoor(target));
    }

    private IEnumerator moveDoor(Vector3 target)
    {
        Vector3 from = doorObj.transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < movementTime)
        {
            elapsedTime += Time.deltaTime;
            doorObj.transform.position = Vector3.Lerp(from, target, elapsedTime / movementTime);
            yield return null;
        }

        doorObj.transform.position = target;
        doorRoutine = null;
    }

}













