// LayoutBuilder.cs v2 — drop into Assets/Editor/ (replaces previous version)
// Tools > Build Designed Level. Rebuilds clean under "GeneratedLayout" each run.
//
// v2 changes:
//  - Walls now generated on ALL solid/walkable boundaries ('#' perimeters and F<->'#' included)
//  - Stair high-end is MEASURED from mesh vertices, then aimed at the landing and flush-fit
//  - Anti-z-fight offsets: walls +4mm up, stairs +6mm, doors +4mm; walls nudged 2mm/floor sideways
//  - 'D' on plain floor spawns the Door prefab in the opening; 'D' inside a room block sets gate side
//  - Per-floor slab prefab (ground floors thick, balconies thin) — edit FloorSlabPrefab below
//  - Fixed deprecated FindObjectsOfType + the inspector MissingReferenceException (selection clear)

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LayoutBuilder
{
    // ================= EDIT ME =================
    // Row 0 = NORTH edge as you have it. '.' empty  '#' solid mass  'F' floor  'S' stairs up
    // 'E'/'P' rooms (2x2, a 'D' inside the block marks the gate side)  'D' on floor = doorway
    static readonly string[] Floor0 = {
        ".##.......",
        ".##.......",
        "########..",
        "##########",
        "##########",
        "########..",
        "########..",
        "########..",
        "########..",
        ".###FS....",
        ".###FF....",
        "....DE....",
        "....EE....",
    };
    static readonly string[] Floor0_5 = {
        ".##.......",
        ".##.......",
        "FFFFFF##..",
        "FFFFFS####",
        "FFFFFF####",
        "FSFFF###..",
        "########..",
        "########..",
        "########..",
        ".#####....",
        ".#####....",
        "....##....",
        "....##....",
    };
    static readonly string[] Floor1 = {
        ".##.......",
        ".##.......",
        "######FF..",
        "######FFEE",
        "######FFEE",
        "#####FFF..",
        "SFFFFFSF..",
        "FFFFFFFF..",
        "FFFFFFFF..",
        ".PPF##....",
        ".PPF##....",
        "....##....",
        "....##....",
    };
    static readonly string[] Floor2 = {
        ".EE.......",
        ".ED.......",
        "FFFFFFFF..",
        "F######F##",
        "F######F##",
        "F######F..",
        "#######F..",
        "########..",
        "########..",
        ".#####....",
        ".#####....",
        "....##....",
        "....##....",
    };

    const float Cell = 5f;
    static readonly float[] FloorY = { 0f, 2f, 3f, 6f };
    const float OriginX = -40f, OriginZ = -30f;

    // Per-floor slab prefab: ground floors thick, balconies/walkways thin. Edit freely.
    static readonly string[] FloorSlabPrefab = { "FloorInteriorA6", "BalconyFloor", "FloorInteriorA6", "BalconyFloor" };
    const string WallLong = "WallLargeB", WallShort = "WallSmallC";
    const string RailPrefab = "HandRailDestructable";          // balcony inner edges get rails, not walls
    // Floor 0.5 gets NO perimeter walls: floor 0's and floor 1's wall rows already cover that band.
    static readonly bool[] BuildPerimeterWalls = { true, false, true, true };
    static readonly bool[] InnerEdgeGetsRail = { false, true, true, true }; // F<->'#' edges: rail above ground, wall on ground
    const string StairTall = "StairsA", StairShort = "StairsB";
    const float StairTallRise = 3f, StairShortRise = 2f;   // geometric rise of each stair mesh
    const string DoorPrefab = "GateWide";                  // change to your own door prefab name
    const string EnemyRoom = "EnemySpawnRoom", PlayerRoom = "PlayerSpawnRoom";
    const Side RoomGateAtYaw0 = Side.South;                // flip this if all room gates face wrong

    // anti-z-fight lifts (meters)
    const float WallLift = 0.004f, StairLift = 0.006f, DoorLift = 0.004f, RoomLift = 0.01f;
    const float FloorWallSideNudge = 0.002f;               // per-floor-index lateral wall nudge
    // ===========================================

    enum Side { North, East, South, West }
    static int Deg(Side s) => s == Side.North ? 0 : s == Side.East ? 90 : s == Side.South ? 180 : 270;

    static readonly string[][] Grids = new string[4][];
    static Transform root;
    static readonly Dictionary<string, GameObject> prefabCache = new();
    static readonly Dictionary<(string, int), Bounds> boundsCache = new();

    [MenuItem("Tools/Build Designed Level")]
    public static void Build()
    {
        Grids[0] = Floor0; Grids[1] = Floor0_5; Grids[2] = Floor1; Grids[3] = Floor2;
        prefabCache.Clear(); boundsCache.Clear();
        Selection.objects = Array.Empty<UnityEngine.Object>();  // deselect before destroying
        Selection.activeObject = null;

        var old = GameObject.Find("GeneratedLayout");
        if (old != null) Undo.DestroyObjectImmediate(old);
        var rootGo = new GameObject("GeneratedLayout");
        Undo.RegisterCreatedObjectUndo(rootGo, "Build Designed Level");
        root = rootGo.transform;

        for (int f = 0; f < 4; f++) BuildFloorSlabs(f);
        for (int f = 0; f < 4; f++) BuildWalls(f);
        for (int f = 0; f < 4; f++) BuildStairs(f);
        PlaceRooms();
        Debug.Log("[LayoutBuilder] Done. Re-run to rebuild; delete 'GeneratedLayout' to remove; Ctrl+Z to undo.");
    }

    // ---------- grid ----------
    static int Rows(int f) => Grids[f].Length;
    static int Cols(int f) => Grids[f][0].Length;
    static char At(int f, int c, int r)
    {
        if (f < 0 || f > 3 || c < 0 || r < 0 || r >= Rows(f) || c >= Cols(f)) return '.';
        return Grids[f][r][c];                              // row 0 = north, matches your grids
    }
    static float CellX(int c) => OriginX + Cell * c;
    static float CellZ(int f, int r) => OriginZ + Cell * (Rows(f) - 1 - r);
    static bool Walk(char k) => k == 'F' || k == 'S' || k == 'D';
    static bool Room(char k) => k == 'E' || k == 'P';
    static bool Solid(char k) => Walk(k) || k == '#';

    // ---------- prefabs & measurement ----------
    static GameObject Prefab(string name)
    {
        if (prefabCache.TryGetValue(name, out var p) && p != null) return p;
        foreach (var guid in AssetDatabase.FindAssets($"t:Prefab {name}"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
            { p = AssetDatabase.LoadAssetAtPath<GameObject>(path); prefabCache[name] = p; return p; }
        }
        Debug.LogError($"[LayoutBuilder] Prefab not found: {name}");
        return null;
    }

    static GameObject Probe(string name, int yaw)
    {
        var probe = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name));
        probe.hideFlags = HideFlags.HideAndDontSave;
        probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
        return probe;
    }

    static Bounds Measure(string name, int yaw)
    {
        if (boundsCache.TryGetValue((name, yaw), out var b)) return b;
        var probe = Probe(name, yaw);
        var rends = probe.GetComponentsInChildren<Renderer>();
        b = rends.Length > 0 ? rends[0].bounds : new Bounds(Vector3.zero, Vector3.one);
        foreach (var rd in rends) b.Encapsulate(rd.bounds);
        UnityEngine.Object.DestroyImmediate(probe);
        boundsCache[(name, yaw)] = b;
        return b;
    }

    // Which compass side holds the stair's HIGH end at yaw 0 — measured from mesh vertices.
    static Side MeasureHighSide(string name)
    {
        var probe = Probe(name, 0);
        var pts = new List<Vector3>();
        foreach (var mf in probe.GetComponentsInChildren<MeshFilter>())
            if (mf.sharedMesh != null)
                pts.AddRange(mf.sharedMesh.vertices.Select(v => mf.transform.TransformPoint(v)));
        var bb = Measure(name, 0);
        UnityEngine.Object.DestroyImmediate(probe);
        float cut = bb.max.y - bb.size.y * 0.25f;
        var high = pts.Where(p => p.y > cut).ToList();
        if (high.Count == 0) return Side.North;
        var d = high.Aggregate(Vector3.zero, (a, p) => a + p) / high.Count - bb.center;
        if (Mathf.Abs(d.x) > Mathf.Abs(d.z)) return d.x > 0 ? Side.East : Side.West;
        return d.z > 0 ? Side.North : Side.South;
    }

    // Place so measured bounds-min lands at (x, yMin, z)
    static GameObject Place(string name, int yaw, float x, float yMin, float z, string label)
    {
        var b = Measure(name, yaw);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(name), root);
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        go.transform.position = new Vector3(x, yMin, z) - b.min;   // pivot at origin during measure
        go.name = label;
        Undo.RegisterCreatedObjectUndo(go, "Build Designed Level");
        return go;
    }

    // ---------- floors ----------
    static void BuildFloorSlabs(int f)
    {
        string prefab = FloorSlabPrefab[f];
        var b = Measure(prefab, 0);
        for (int r = 0; r < Rows(f); r++)
            for (int c = 0; c < Cols(f); c++)
            {
                if (!Walk(At(f, c, r))) continue;
                Place(prefab, 0, CellX(c), FloorY[f] - b.size.y, CellZ(f, r), $"F{f}_slab_{c}_{r}");
            }
    }

    // ---------- walls ----------
    // 0 = nothing, 1 = wall, 2 = rail
    static int EdgeKind(int f, char a, char b)
    {
        if (a == 'D' || b == 'D') return 0;
        if (Room(a) || Room(b)) return 0;                  // handled by RoomWalls()
        if ((a == 'S' && Walk(b)) || (b == 'S' && Walk(a))) return 0;
        bool perim = (Solid(a) && b == '.') || (Solid(b) && a == '.');
        if (perim) return BuildPerimeterWalls[f] ? 1 : 0;
        bool inner = (Walk(a) && b == '#') || (Walk(b) && a == '#');
        if (inner) return InnerEdgeGetsRail[f] ? 2 : 1;
        return 0;
    }

    static void BuildWalls(int f)
    {
        float baseY = FloorY[f] + WallLift;
        float nudge = FloorWallSideNudge * f;
        // vertical boundaries: plane x = CellX(c)
        for (int c = 0; c <= Cols(f); c++)
        {
            var run = new List<int>(); int runKind = 0;
            for (int r = 0; r <= Rows(f); r++)
            {
                int kind = r < Rows(f) ? EdgeKind(f, At(f, c - 1, r), At(f, c, r)) : 0;
                if (kind != 0 && (run.Count == 0 || kind == runKind)) { run.Add(r); runKind = kind; }
                else
                {
                    if (run.Count > 0) { EmitRun(f, true, c, run, runKind, baseY, nudge); run.Clear(); }
                    if (kind != 0) { run.Add(r); runKind = kind; }
                }
            }
            if (run.Count > 0) EmitRun(f, true, c, run, runKind, baseY, nudge);
        }
        // horizontal boundaries: plane z at north edge of row r
        for (int r = 0; r <= Rows(f); r++)
        {
            var run = new List<int>(); int runKind = 0;
            for (int c = 0; c <= Cols(f); c++)
            {
                int kind = c < Cols(f) ? EdgeKind(f, At(f, c, r), At(f, c, r - 1)) : 0;
                if (kind != 0 && (run.Count == 0 || kind == runKind)) { run.Add(c); runKind = kind; }
                else
                {
                    if (run.Count > 0) { EmitRun(f, false, r, run, runKind, baseY, nudge); run.Clear(); }
                    if (kind != 0) { run.Add(c); runKind = kind; }
                }
            }
            if (run.Count > 0) EmitRun(f, false, r, run, runKind, baseY, nudge);
        }
        RoomWalls(f, baseY);
    }

    // enclose each room block: walls on three sides, door prefab on the gate side
    static void RoomWalls(int f, float baseY)
    {
        var seen = new HashSet<(int, int)>();
        for (int r = 0; r < Rows(f); r++)
            for (int c = 0; c < Cols(f); c++)
            {
                if (!Room(At(f, c, r)) || seen.Contains((c, r))) continue;
                var (cMin, rMin, gate) = RoomBlock(f, c, r);
                for (int dc = 0; dc < 2; dc++) for (int dr = 0; dr < 2; dr++) seen.Add((cMin + dc, rMin + dr));
                float x0 = CellX(cMin), z0 = CellZ(f, rMin + 1);   // south-west corner of the 2x2 block
                foreach (Side s in Enum.GetValues(typeof(Side)))
                {
                    bool vertical = s == Side.East || s == Side.West;
                    float px = s == Side.West ? x0 : s == Side.East ? x0 + 2 * Cell : x0;
                    float pz = s == Side.South ? z0 : s == Side.North ? z0 + 2 * Cell : z0;
                    if (s == gate)
                    {
                        int dyaw = vertical ? 90 : 0;
                        var db = Measure(DoorPrefab, dyaw);
                        float dx = vertical ? px - db.size.x * 0.5f : x0 + Cell - db.size.x * 0.5f;
                        float dz = vertical ? z0 + Cell - db.size.z * 0.5f : pz - db.size.z * 0.5f;
                        Place(DoorPrefab, dyaw, dx, FloorY[f] + DoorLift, dz, $"F{f}_roomGate_{cMin}_{rMin}_{s}");
                        continue;
                    }
                    var wb = Measure(WallLong, vertical ? 90 : 0);
                    float wx = vertical ? px - wb.size.x * 0.5f : x0;
                    float wz = vertical ? z0 : pz - wb.size.z * 0.5f;
                    Place(WallLong, vertical ? 90 : 0, wx, baseY, wz, $"F{f}_roomWall_{cMin}_{rMin}_{s}");
                }
            }
    }

    static (int cMin, int rMin, Side gate) RoomBlock(int f, int c, int r)
    {
        var block = new List<(int c, int r, char k)>();
        for (int dc = -1; dc <= 1; dc++) for (int dr = -1; dr <= 1; dr++)
        {
            char kk = At(f, c + dc, r + dr);
            if (Room(kk) || kk == 'D') block.Add((c + dc, r + dr, kk));
        }
        int cMin = block.Min(t => t.c), rMin = block.Min(t => t.r);
        Side gate = Side.South; bool set = false;
        foreach (var t in block.Where(t => t.k == 'D'))
        {
            if (Walk(At(f, t.c, t.r - 1)) || At(f, t.c, t.r - 1) == '.') { gate = t.r == rMin ? Side.North : Side.South; set = true; }
            if (t.c == cMin && (Walk(At(f, cMin - 1, t.r)) || At(f, cMin - 1, t.r) == '.')) { gate = Side.West; set = true; }
            if (t.c == cMin + 1 && (Walk(At(f, cMin + 2, t.r)) || At(f, cMin + 2, t.r) == '.')) { gate = Side.East; set = true; }
        }
        if (!set)
        {
            if (Walk(At(f, cMin, rMin + 2)) || Walk(At(f, cMin + 1, rMin + 2))) gate = Side.South;
            else if (Walk(At(f, cMin, rMin - 1)) || Walk(At(f, cMin + 1, rMin - 1))) gate = Side.North;
            else if (Walk(At(f, cMin - 1, rMin)) || Walk(At(f, cMin - 1, rMin + 1))) gate = Side.West;
            else if (Walk(At(f, cMin + 2, rMin)) || Walk(At(f, cMin + 2, rMin + 1))) gate = Side.East;
        }
        return (cMin, rMin, gate);
    }

    static void EmitRun(int f, bool vertical, int line, List<int> idx, int kind, float baseY, float nudge)
    {
        int i = 0;
        while (i < idx.Count)
        {
            bool rail = kind == 2;
            bool ten = !rail && i + 1 < idx.Count && idx[i + 1] == idx[i] + 1;
            string prefab = rail ? RailPrefab : (ten ? WallLong : WallShort);
            if (vertical)
            {
                var b = Measure(prefab, 90);
                float x = CellX(line) - b.size.x * 0.5f + nudge;
                float zTopRow = Mathf.Min(idx[i], ten ? idx[i + 1] : idx[i]) + (ten ? 1 : 0);
                float z = CellZ(f, idx[i] + (ten ? 1 : 0));       // south end of the run
                Place(prefab, 90, x, baseY, z, $"F{f}_wallV_{line}_{idx[i]}");
            }
            else
            {
                var b = Measure(prefab, 0);
                float z = CellZ(f, line) + Cell - b.size.z * 0.5f + nudge; // centered on north edge of row 'line'
                Place(prefab, 0, CellX(idx[i]), baseY, z - 0f, $"F{f}_wallH_{idx[i]}_{line}");
            }
            i += ten ? 2 : 1;
        }
        // doors on D-cell openings along this line are handled in BuildDoors via cell scan
    }

    // doors: scan D cells that are NOT part of a room block
    static void BuildDoors(int f)
    {
        for (int r = 0; r < Rows(f); r++)
            for (int c = 0; c < Cols(f); c++)
            {
                if (At(f, c, r) != 'D') continue;
                bool nearRoom = Room(At(f, c + 1, r)) || Room(At(f, c - 1, r)) || Room(At(f, c, r + 1)) || Room(At(f, c, r - 1));
                if (nearRoom) continue;                    // room-gate marker, room handles it
                foreach (var (side, dc, dr) in new[] { (Side.North, 0, -1), (Side.South, 0, 1), (Side.East, 1, 0), (Side.West, -1, 0) })
                {
                    char k = At(f, c + dc, r + dr);
                    if (k != '.' && k != '#') continue;    // door sits in the wall line
                    int yaw = (side == Side.East || side == Side.West) ? 90 : 0;
                    var b = Measure(DoorPrefab, yaw);
                    float x = CellX(c) + (side == Side.East ? Cell - b.size.x * 0.5f : side == Side.West ? -b.size.x * 0.5f : (Cell - b.size.x) * 0.5f);
                    float z = CellZ(f, r) + (side == Side.North ? Cell - b.size.z * 0.5f : side == Side.South ? -b.size.z * 0.5f : (Cell - b.size.z) * 0.5f);
                    Place(DoorPrefab, yaw, x, FloorY[f] + DoorLift, z, $"F{f}_door_{c}_{r}_{side}");
                }
            }
    }

    // ---------- stairs ----------
    static void BuildStairs(int f)
    {
        BuildDoors(f);
        if (f >= 3) return;
        float rise = FloorY[f + 1] - FloorY[f];
        string prefab = rise > StairShortRise + 0.2f ? StairTall : StairShort;
        float meshRise = prefab == StairTall ? StairTallRise : StairShortRise;
        Side high0 = MeasureHighSide(prefab);
        for (int r = 0; r < Rows(f); r++)
            for (int c = 0; c < Cols(f); c++)
            {
                if (At(f, c, r) != 'S') continue;
                Side landing = default; bool found = false;
                foreach (var (s, dc, dr) in new[] { (Side.North, 0, -1), (Side.South, 0, 1), (Side.East, 1, 0), (Side.West, -1, 0) })
                    if (Walk(At(f + 1, c + dc, r + dr))) { landing = s; found = true; break; }
                if (!found)
                {
                    Debug.LogWarning($"[LayoutBuilder] Stair at floor {f} cell ({c},{r}): no landing above on floor {f + 1} — skipped. Add an F above it or remove the S.");
                    continue;
                }
                int yaw = ((Deg(landing) - Deg(high0)) % 360 + 360) % 360;
                var b = Measure(prefab, yaw);
                // flush the high end against the landing-side cell edge, centered on the other axis
                float x = (landing == Side.East) ? CellX(c) + Cell - b.size.x
                        : (landing == Side.West) ? CellX(c)
                        : CellX(c) + (Cell - b.size.x) * 0.5f;
                float z = (landing == Side.North) ? CellZ(f, r) + Cell - b.size.z
                        : (landing == Side.South) ? CellZ(f, r)
                        : CellZ(f, r) + (Cell - b.size.z) * 0.5f;
                float yMin = FloorY[f + 1] - meshRise + StairLift;   // top step meets the landing
                Place(prefab, yaw, x, yMin, z, $"F{f}_stairs_{c}_{r}");
                if (Mathf.Abs(rise - meshRise) > 0.05f)
                    Debug.Log($"[LayoutBuilder] Stair at floor {f} ({c},{r}): mesh rise {meshRise} vs gap {rise} — base sinks {meshRise - rise:0.##}m into the lower floor (no matching stair in the pack).");
            }
    }

    // ---------- rooms ----------
    static void PlaceRooms()
    {
        var enemies = new Queue<GameObject>(FindByPrefix(EnemyRoom));
        var players = new Queue<GameObject>(FindByPrefix(PlayerRoom));
        for (int f = 0; f < 4; f++)
        {
            var seen = new HashSet<(int, int)>();
            for (int r = 0; r < Rows(f); r++)
                for (int c = 0; c < Cols(f); c++)
                {
                    char k = At(f, c, r);
                    if (!Room(k) || seen.Contains((c, r))) continue;
                    // find the 2x2 block (E/P plus optional D member)
                    var block = new List<(int c, int r, char k)>();
                    for (int dc = -1; dc <= 1; dc++) for (int dr = -1; dr <= 1; dr++)
                    {
                        char kk = At(f, c + dc, r + dr);
                        if (Room(kk) || kk == 'D') block.Add((c + dc, r + dr, kk));
                    }
                    int cMin = block.Min(t => t.c), rMin = block.Min(t => t.r);
                    for (int dc = 0; dc < 2; dc++) for (int dr = 0; dr < 2; dr++) seen.Add((cMin + dc, rMin + dr));
                    // gate side: D member's outward edge, else the side touching walkable floor
                    Side gate = Side.South; bool set = false;
                    foreach (var t in block.Where(t => t.k == 'D'))
                    {
                        if (t.r == rMin) { gate = Side.North; set = true; }
                        else if (t.r == rMin + 1) { gate = Side.South; set = true; }
                        if (t.c == cMin && Walk(At(f, cMin - 1, t.r))) { gate = Side.West; }
                        if (t.c == cMin + 1 && Walk(At(f, cMin + 2, t.r))) { gate = Side.East; }
                    }
                    if (!set)
                        foreach (var (s, dc, dr) in new[] { (Side.South, 0, 2), (Side.North, 0, -1), (Side.West, -1, 0), (Side.East, 2, 0) })
                            if (Walk(At(f, cMin + Math.Max(0, Math.Min(1, dc)), rMin + dr))) { gate = s; break; }
                    int yaw = ((Deg(gate) - Deg(RoomGateAtYaw0)) % 360 + 360) % 360;
                    bool isEnemy = block.Any(t => t.k == 'E');
                    var pool = isEnemy ? enemies : players;
                    GameObject room = pool.Count > 0 ? pool.Dequeue()
                        : (GameObject)PrefabUtility.InstantiatePrefab(Prefab(isEnemy ? EnemyRoom : PlayerRoom), root);
                    Undo.RecordObject(room.transform, "Build Designed Level");
                    room.transform.rotation = Quaternion.Euler(0, yaw, 0);
                    float czTop = CellZ(f, rMin);                       // north row's south corner
                    room.transform.position = new Vector3(CellX(cMin) + Cell, FloorY[f] + RoomLift, CellZ(f, rMin + 1) + Cell);
                    Debug.Log($"[LayoutBuilder] {room.name}: floor {f}, gate {gate} (yaw {yaw}). Wrong side for all rooms? Edit RoomGateAtYaw0.");
                }
        }
    }

    static List<GameObject> FindByPrefix(string prefix) =>
        UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
            .Where(t => t != null && t.name.StartsWith(prefix) && (root == null || !t.IsChildOf(root)))
            .Select(t => t.gameObject).Distinct().ToList();
}
#endif