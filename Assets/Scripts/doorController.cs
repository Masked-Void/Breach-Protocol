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
            StartCoroutine(OpenDoor());
            runOpen = false;
        }

        if (runClose)
        {
            StartCoroutine(CloseDoor());
            runClose = false;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")&& other.GetComponent<EnemyBase>().hasLeftSpawnRoom==false)
        {
            StartCoroutine(OpenDoor());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy") && other.GetComponent<EnemyBase>().hasLeftSpawnRoom == false)
        {
            StartCoroutine(CloseDoor());
            other.GetComponent<EnemyBase>().hasLeftSpawnRoom = true;
        }
    }

    private IEnumerator OpenDoor()
    {

        float elapsedTime = 0f;

        while (elapsedTime < movementTime)
        {

            elapsedTime += Time.deltaTime;

            float percentComplete = elapsedTime / movementTime;

            doorObj.transform.position = Vector3.Lerp(startPos, endPos, percentComplete);

            yield return null;

        }
       
    }

    private IEnumerator CloseDoor()
    {
        float elapsedTime = 0f;

        while (elapsedTime < movementTime)
        {
            elapsedTime += Time.deltaTime;

            float percentComplete = elapsedTime / movementTime;

            doorObj.transform.position = Vector3.Lerp(endPos, startPos, percentComplete);

            yield return null;
        }

    }
}













