using UnityEngine;
using UnityEngine.AI;

public class ClassSpawner : MonoBehaviour
{
    [SerializeField] GameObject objectToSpawn;
    [SerializeField] int amountToSpawn;
    [SerializeField] int spawnRate;
    [SerializeField] int spawnDist;

    [SerializeField] ParticleSystem spawnEffect;

    int spawnCount;
    float spawnTimer;

    bool startSpawning;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager.instance.updateGameGoal(amountToSpawn);
    }

    // Update is called once per frame
    void Update()
    {
        if (startSpawning)
        {
            spawnTimer += Time.deltaTime;

            if (spawnCount < amountToSpawn && spawnTimer > spawnRate)
            {
                spawn();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            startSpawning = true;
        }
    }

    void spawn()
    {
        spawnTimer = 0;
        spawnCount++;

        Vector3 ranPos = Random.insideUnitSphere * spawnDist;
        ranPos += transform.position;

        NavMeshHit hit;
        NavMesh.SamplePosition(ranPos, out hit, spawnDist, 1);

        Instantiate(objectToSpawn, hit.position, Quaternion.Euler(0, Random.Range(0, 360), 0));
        if (spawnEffect != null)
            Instantiate(spawnEffect, hit.position, Quaternion.identity);
    }
}
