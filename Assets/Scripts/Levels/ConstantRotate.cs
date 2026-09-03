using UnityEngine;

// spins an object on its Y axis forever. decoration only, no gameplay effect.
public class ConstantRotate : MonoBehaviour
{
    [Tooltip("degrees per second, negative spins the other way")]
    [SerializeField] int speed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up, speed * Time.deltaTime);
    }
}