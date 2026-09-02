using UnityEngine;

// a hold point in the boss arena. the player stands inside a cylinder to fill progress, and
// leaving drains it slowly instead of wiping it. everything in here runs on unscaled time
// because nothing in the boss fight obeys the player's time scale.
public class HoldZone : MonoBehaviour {

    public enum holdMode { breakImmunity, dealDamage }

    public enum fillAxis { vertical, horizontal }

    [Header("Mode")]
    [Tooltip("breakImmunity or dealDamage")]
    [SerializeField] private holdMode mode = holdMode.dealDamage;
    [SerializeField] private float holdDamageAmount = 100f;

    [Header("Shape")]
    [Tooltip("Flat X-Z reach from the center object")]
    [SerializeField] private float radius = 4f;
    [Tooltip("How tall the zone is")]
    [SerializeField] private float height = 3f;
    [Tooltip("Moves the base of the zone if it goes beneath the map")]
    [SerializeField] private float baseOffset = 0f;


    [Header("Timing")]
    [Tooltip("Seconds inside the zone to go from full to empty")]
    [SerializeField] private float fillTime = 10f;
    [Tooltip("Drain speed of progress")]
    [SerializeField] private float drainMult = .35f;


    [Header("Computer Screen")]
    [Tooltip("the bar on the computer screen. it gets scaled by progress, so set the sprite's pivot to whichever edge the bar should grow out of")]
    [SerializeField] private SpriteRenderer fillSprite;
    [Tooltip("which axis the bar stretches along")]
    [SerializeField] private fillAxis growAxis = fillAxis.horizontal;
    [Tooltip("HDR, so crank the intensity and the screen blooms")]
    [ColorUsage(true , true)][SerializeField] private Color fillingColor = new Color(0.2f , 0.9f , 1f);
    [ColorUsage(true , true)][SerializeField] private Color drainingColor = new Color(1f , 0.25f , 0.2f);
    [Tooltip("color multiplier at full progress, so the screen gets brighter as it fills and reads from across the arena")]
    [SerializeField] private float brightnessAtFull = 4f;


    [Header("Zone Marker")]
    [Tooltip("Ring that shows where the bounds are")]
    [SerializeField] private Transform zoneMarker;
    [Tooltip("Child object that gets turned on")]
    [SerializeField] private GameObject visuals;


    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";


    // Manager reads these
    public float progress { get; private set; }
    public bool playerInside { get; private set; }
    public bool isActive { get; private set; }
    public holdMode zoneMode { get { return mode; } }
    public float damageAmt { get { return holdDamageAmount; } }


    private HoldZoneManager owner;
    private bool reported;

    private Vector3 barBaseScale = Vector3.one;

    void Awake() {
        if (player == null) {
            GameObject gameObj = GameObject.FindGameObjectWithTag(playerTag);
            if (gameObj != null)
                player = gameObj.transform;
        }

        if (player == null) {
            Debug.LogError("HoldZone: no player reference and nothing is tagger: " + playerTag , this);
        }

        if (fillSprite != null)
            barBaseScale = fillSprite.transform.localScale;


        isActive = false;
        if (visuals != null)
            visuals.SetActive(false);
    }


    void OnValidate() {
        matchMarkerToRadius();
    }

    void Update() {
        if (!isActive || player == null)
            return;

        playerInside = checkInside();

        // fills while standing in and slowly decreases while not
        float rate = 1f / Mathf.Max(0.01f , fillTime);
        if (playerInside)
            progress += rate * Time.unscaledDeltaTime;
        else
            progress -= rate * drainMult * Time.unscaledDeltaTime;
        progress = Mathf.Clamp01(progress);

        updateScreen();

        // Reports to the manager that the zone has been completed
        if (progress >= 1f && !reported) {
            reported = true;
            if (owner != null)
                owner.holdComplete(this);
        }
    }


    bool checkInside() {
        Vector3 basePos = transform.position + Vector3.up * baseOffset;
        Vector3 offset = player.position - basePos;

        // Height check
        if (offset.y < 0 || offset.y > height)
            return false;
        offset.y = 0;

        return offset.sqrMagnitude <= radius * radius;
    }


    // Stretches sprite
    void updateScreen() {
        if (fillSprite == null)
            return;

        // Scales one axis only
        Vector3 scale = barBaseScale;

        if (growAxis == fillAxis.vertical)
            scale.y = barBaseScale.y * progress;
        else
            scale.x = barBaseScale.x * progress;

        fillSprite.transform.localScale = scale;

        Color state = playerInside ? fillingColor : drainingColor;
        Color lit = state * Mathf.Lerp(1f , brightnessAtFull , progress);

        lit.a = state.a;
        fillSprite.color = lit;
    }


    void matchMarkerToRadius() {
        if (zoneMarker == null)
            return;

        zoneMarker.localScale = new Vector3(radius * 2f , zoneMarker.localScale.y , radius * 2f);
    }


    public void activate(HoldZoneManager manager) {
        owner = manager;
        progress = 0f;
        playerInside = false;
        reported = false;
        isActive = true;

        matchMarkerToRadius();

        if (visuals != null) {
            visuals.SetActive(true);
        }

        updateScreen();
    }

    public void deactivate() {
        isActive = false;
        playerInside = false;
        progress = 0f;
        owner = null;

        if (visuals != null)
            visuals.SetActive(false);

        updateScreen();
    }


    [ContextMenu("Fill Hold")]
    void debugFill() {
        progress = 1f;
        updateScreen();
    }

    [ContextMenu("Empty Hold")]
    void debugEmpty() {
        progress = 0f;
        reported = false;
        updateScreen();
    }

    void OnDrawGizmos() {
        Gizmos.color = isActive ? Color.cyan : Color.magenta;

        Vector3 bottom = transform.position + Vector3.up * baseOffset;
        Vector3 top = bottom + Vector3.up * height;

        drawRing(bottom);
        drawRing(top);
    }


    void drawRing(Vector3 center) {
        int steps = 32;
        Vector3 prev = center + new Vector3(radius , 0 , 0);

        for (int i = 1 ; i <= steps ; i++) {
            float area = (i / (float)steps) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(area) , 0f , Mathf.Sin(area)) * radius;
            Gizmos.DrawLine(prev , next);
            prev = next;
        }
    }
}