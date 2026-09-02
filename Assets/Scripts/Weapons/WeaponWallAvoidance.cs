using UnityEngine;

public class WeaponWallAvoidance : MonoBehaviour
{
    Transform clipProjector; 
    public LayerMask clipLayer; 

    [Header("Settings")]
    public float checkDist = 1f;
    public Vector3 newDir = new Vector3(0f, -90f, 0f);

    private float lerpPos;
    private Quaternion defaultRot;
    private Quaternion clippedRot;

    private void Start()
    {
        defaultRot = Quaternion.identity;
        clippedRot = Quaternion.Euler(newDir);
        clipProjector = GameObject.FindWithTag("Clipper").transform;
    }

    private void Update()
    {
        if (Physics.Raycast(clipProjector.position, clipProjector.forward, out RaycastHit hit, checkDist, clipLayer))
        {
            lerpPos = 1f - (hit.distance / checkDist);
        }
        else
        {
            lerpPos = 0f;
        }
        
        lerpPos = Mathf.Clamp01(lerpPos);

        transform.localRotation = Quaternion.Lerp(defaultRot, clippedRot, lerpPos);
    }
}