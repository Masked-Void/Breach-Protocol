using UnityEngine;

/*
 * Script: DissolveWallVisuals
 *
 * Description:
 * Drives the radial alpha clip shader on one wall face. The hole grows from its
 * center, and the edge glow brightens as time speeds up, so the wall shows the
 * player their own speed even between bursts.
 *
 * Interacts With:
 * - DissolveWall (ProgressChanged, CenterPosition, AlongWall, MaxRadius)
 * - TimeManager (MinTimeScale, where the glow range starts)
 *
 * Notes:
 * - One MaterialPropertyBlock per face, so faces never copy the material.
 * - The burst's main module must run on scaled time, or bursts ignore the lesson.
 */
[RequireComponent(typeof(Renderer))]
public class DissolveWallVisuals : MonoBehaviour
{
    // tries at finding the edge point on the face
    private const int BurstPlacementTries = 8;

    [Header("Source")]

    [Tooltip("The face this visual follows")]
    [SerializeField] private DissolveWall wall;


    [Header("Shader")]

    [Tooltip("Shader float for the hole radius in meters")]
    [SerializeField] private string radiusProperty = "DissolveRadius";

    [Tooltip("shader vector for the hole center in world space")]
    [SerializeField] private string centerProperty = "DissolveCenter";

    [Tooltip("shader float for the edge glow strength")]
    [SerializeField] private string glowProperty = "EdgeGlow";


    [Header("Edge Bursts")]

    [Tooltip("glow at the slowest time scale")]
    [SerializeField] private float minGlow = 0.2f;

    [Tooltip("glow at full time scale")]
    [SerializeField] private float maxGlow = 3f;

    [Tooltip("only used if TimeManager is missing")]
    [SerializeField] private float fallbackTimeScaleFloor = 0.1f;


    [Header("Edge Bursts")]

    [Tooltip("cube burst on the hole's edge, its main module must use scaled time")]
    [SerializeField] private ParticleSystem edgeBurst;

    [Tooltip("Progress between bursts, smaller means more pops")]
    [Range(0.005f, 0.1f)]
    [SerializeField] private float burstStep = 0.02f;

    [Tooltip("cubes per burst")]
    [SerializeField] private int cubesPerBurst = 6;

    [Tooltip("turns bursts off, for reduced motion setting")]
    [SerializeField] private bool isReducedMotion = false;

    private MaterialPropertyBlock block;
    private Renderer faceRenderer;
    private int radiusId;
    private int centerId;
    private int glowId;
    private float lastBurstProgress;

    private void Awake()
    {
        faceRenderer = GetComponent<Renderer>();
        block = new MaterialPropertyBlock();
        radiusId = Shader.PropertyToID(radiusProperty);
        centerId = Shader.PropertyToID(centerProperty);
        glowId = Shader.PropertyToID(glowProperty);
    }

    private void OnEnable()
    {
        if (wall != null)
        {
            wall.ProgressChanged.AddListener(handleProgress);
        }
    }

    private void OnDisable()
    {
        if (wall!= null)
        {
            wall.ProgressChanged.RemoveListener(handleProgress);
        }
    }

    private void Start()
    {
        if (wall != null)
        {
            lastBurstProgress = wall.ShapedProgress;
            applyProgress(wall.ShapedProgress);
        }
    }


    private void Update()
    {
        block.SetFloat(glowId, glowForTimeScale(Time.timeScale));
        faceRenderer.SetPropertyBlock(block);
    }

    private void handleProgress(float shappedProgress)
    {
        applyProgress(shappedProgress);

        if (shappedProgress < lastBurstProgress)
        {
            lastBurstProgress = shappedProgress;
            return;
        }

        while (shappedProgress - lastBurstProgress >= burstStep)
        {
            lastBurstProgress += burstStep;
            emitBurst();
        }
    }

    private void applyProgress(float shappedProgress)
    {
        block.SetFloat(radiusId, shappedProgress*wall.MaxRadius);
        block.SetVector(centerId, wall.CenterPosition);
        faceRenderer.SetPropertyBlock(block);
    }

    private float glowForTimeScale(float timeScale)
    {
        float floor = TimeManager.instance != null ? TimeManager.instance.MinTimeScale : fallbackTimeScaleFloor;
        return Mathf.Lerp(minGlow, maxGlow, Mathf.InverseLerp(floor, 1f, timeScale));
    }

    private void emitBurst()
    {
        if (isReducedMotion || edgeBurst == null || wall == null)
        {
            return;
        }

        float radius = wall.CurrentRadius;
        for(int i = 0; i < BurstPlacementTries; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 point = wall.CenterPosition + (wall.AlongWall * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle)) * radius;

            if (faceRenderer.bounds.Contains(point))
            {
                edgeBurst.transform.position = point;
                edgeBurst.Emit(cubesPerBurst);
                return;
            }
        }
    }

}
