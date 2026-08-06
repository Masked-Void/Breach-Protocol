using UnityEngine;

public class FaceFollowPlayer : MonoBehaviour
{

    [SerializeField] GameObject followedObject;
    public float rotationOffsetX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationOffsetX = transform.rotation.x;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 objDir = followedObject.transform.position - transform.position;

        Quaternion rot = Quaternion.LookRotation(objDir);

        transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10 * Time.deltaTime);

    }
}
