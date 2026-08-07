using System.Collections;
using UnityEngine;

public class FaceFollowPlayer : MonoBehaviour
{

    [SerializeField] GameObject followedObject;
    public float rotationOffsetX;

    [SerializeField] public bool doShake = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationOffsetX = transform.rotation.x;
    }

    // Update is called once per frame
    void Update()
    {

        if (!doShake)
        {
            Vector3 objDir = followedObject.transform.position - transform.position;

            Quaternion rot = Quaternion.LookRotation(objDir);

            transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10 * Time.deltaTime);
        } else
        {
            StartCoroutine(phaseShake());
        }
    }

    IEnumerator phaseShake()
    {
        float x = Random.Range(-40f, 15f);
        float y = Random.Range(-50f, 50f);
        float z = Random.Range(-5f, 10f);

        Vector3 shakeRot = new Vector3(x, y, z);

        Quaternion shakeQuat = Quaternion.LookRotation(shakeRot);
        
        transform.rotation = Quaternion.Lerp(transform.rotation, shakeQuat, 10 * Time.deltaTime);
        yield return new WaitForSecondsRealtime(0.1f);

        //Quaternion(-0.321928173,0.401794851,0.0272429436,0.85684365)
        //Quaternion(0.0419959202,-0.42629239,0.115382843,0.896213114)
    }
}
