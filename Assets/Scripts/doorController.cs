using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class doorController : MonoBehaviour
{
    [Header("Drag door Obj in")]
    [SerializeField] Transform doorObj;

    [Header("Marker names (children of the door object)")]
    [SerializeField] string closedName = "Closed";
    [SerializeField] string openName = "Open";

    [Header("Speed")]
    [SerializeField] float movementTime = 1;

    [Header("Auto close")]
    [SerializeField] public bool closeWhenEmpty;
    public float closeDelay = 1f;

    Transform closedPos;
    Transform openPos;

    float current;

    Coroutine moveRoutine;

    int insideCount = 0;

    public bool IsOpen
    {
        get
        {
            return current >= 1f;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (doorObj == null)
        {
            Debug.LogError("doorController: doorObj isn't assigned", this);
            enabled = false;
        }

        closedPos = findMark(closedName);
        openPos = findMark(openName);
    }

    Transform findMark(string wanted)
    {
        foreach (Transform child in doorObj)
        {
            if (child.name == wanted)
            {
                return child;
            }
        }

        return null;
    }


    void OnTriggerEnter(Collider other)
    {

        if (!isEnemy(other))
        {
            return;
        }

        insideCount += 1;
        open();

    }

    void OnTriggerExit(Collider other)
    {
        if (!isEnemy(other))
        {
            return;
        }

        insideCount -= 1;

        if (insideCount < 0)
        {
            insideCount = 0;
        }

        
    }

    bool isEnemy(Collider other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return false;
        }

        enemyBase enemy;
        if (!other.TryGetComponent(out enemy))
        {
            return false;
        }

        return !enemy.hasLeftSpawnRoom;
    }

    void open() { }
}













