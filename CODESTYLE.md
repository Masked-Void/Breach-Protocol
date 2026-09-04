# Breach Protocol — Code Style

The rules the codebase follows, and why. Read this before your first commit.

Most of these came out of a real problem. Where that's true, the reason is written down — a rule you understand is one you'll follow when it's inconvenient.

---

# 1. NAMING

| Thing | Convention | Example |
|---|---|---|
| Class, struct, enum, file | `PascalCase` | `GameManager`, `EnemyBase` |
| Interface | `I` + `PascalCase` | `IDamage`, `IWaveHost` |
| Enum values | `PascalCase` | `EnemyType.Basic` |
| Public method | `PascalCase` | `AddBytes()`, `StatePause()` |
| Private method | `camelCase` | `placePlayerAtSpawn()` |
| Public field or property | `PascalCase` | `TotalBytes`, `CurrentLevel` |
| Serialized private field | `camelCase` | `moveSpeed`, `maxBpm` |
| Private field | `camelCase` | `isDead` |
| Constant | `PascalCase` | `MaxRounds` |
| ScriptableObject class | `PascalCase` + `Config` | `StressConfig` |
| Event | `PascalCase`, past tense | `Killed`, `PlayerReady` |
| Coroutine | `PascalCase` verb | `LoadLevel()` |

**Acronyms over two letters are words.** `Bpm` not `BPM`. `Ddos` not `DDOS`. `UI` and `ID` stay uppercase by convention.

**Spell things out.** `position` not `pos`. `damage` not `dmg`. Drop `mgr` entirely.

**Booleans read as questions.** `isDead`, `hasLost`, `canFire`.

**Collections are plural.** `enemies`, `spawnPoints`.

**Put the unit in the name when it's ambiguous.** `stressDecayPerSecond` beats `stressDecayRate`. `delaySeconds` beats `delay`.

**Never name a field the same as its type.** `private TrapManager TrapManager;` compiles and then shadows the type inside that class. Use `private TrapManager trapManager;`.

**Name things for what they are, not what they were.** `simplifiedEnemySpawnDoor` described its history. `clip` sounded like a magazine and actually stops the gun clipping into walls — it's `WeaponWallAvoidance` now.

---

# 2. THE LIFECYCLE RULE

This one prevents actual bugs. Every startup failure during the refactor came from breaking it.

```
Awake()      ONLY yourself. Singleton assignment, GetComponent on your own
             object, initializing your own fields.
             NEVER FindWithTag. NEVER another manager's .instance.

OnEnable()   subscribe to events.
OnDisable()  unsubscribe. Always pair with OnEnable.

Start()      anything that needs another object to exist.
             Still not ordered against other Start methods, so if you depend
             on a specific manager, subscribe to its ready event instead of
             assuming it ran first.

Update()     per frame only. No FindObjectOfType. No GetComponent.
```

**What went wrong.** `GameManager` read the player in `Awake`, but the player is a separate root object in Bootstrap and Unity doesn't guarantee order between root objects. Moving it to `Start` fixed that and broke `WeaponManager`, which read `GameManager.instance.playerScript` in *its* `Start` and silently returned when it was null. No error, no weapons.

**The fix that generalizes:** if you need another manager to have finished, subscribe to an event it raises rather than guessing at order.

```csharp
// GameManager, once it has found the player
public static event System.Action PlayerReady;
if (player != null) PlayerReady?.Invoke();

// WeaponManager
void OnEnable()  => GameManager.PlayerReady += setupWeapons;
void OnDisable() => GameManager.PlayerReady -= setupWeapons;
```

---

# 3. FILE STRUCTURE

**One top-level type per file.** No exceptions. Unity resolves a script reference by matching the file, and with two top-level types it picks the first one declared — which produces `'roamPoint' is missing the class attribute 'ExtensionOfNativeClass'` and Unity silently detaching components to "fix" it.

Nested types are fine. `HoldZone.holdMode` belongs to its parent and doesn't compete for the filename.

**File name must equal the class name.** Unity requires it for any MonoBehaviour or ScriptableObject. Rename both together or the script won't load at all.

**Member order:**

```csharp
public class ThingManager : MonoBehaviour
{
    public static ThingManager instance;

    [Header("Config")]
    [SerializeField] private ThingConfig config;

    [Header("Refs")]
    [SerializeField] private Transform anchor;

    // private runtime state, no attributes
    private bool isRunning;

    // events others subscribe to
    public static event System.Action Ready;

    // read only views
    public bool IsRunning => isRunning;

    private void Awake() { }
    private void OnEnable() { }
    private void OnDisable() { }
    private void Start() { }
    private void Update() { }

    // public API
    public void DoTheThing() { }

    // private helpers
    private void helperThing() { }
}
```

**Nothing over about 300 lines.** Bigger means it does multiple jobs, and multiple people will need to edit it.

**Never reformat or reorder a file you don't own.** A whitespace pass turns a clean merge into a whole-file conflict.

---

# 4. COMMENTS

Three styles, each for a different job.

**Style A — block header.** Managers, systems, anything over ~100 lines.

```csharp
/*
 * Script: LevelLoader
 *
 * Description:
 * Lives in Bootstrap and swaps which level scene is loaded while every manager
 * stays put. Only one level is loaded at a time.
 *
 * Responsibilities:
 * - Load the level the title screen asked for, additively on top of Bootstrap
 * - Unload the previous level before loading a new one
 * - Move the player to the level's Player Spawn Pos marker
 *
 * Interacts With:
 * - LevelBootstrapper (sits in each level scene)
 * - GameManager (player reference)
 * - TitleScreenManager (sets requestedLevel before loading Bootstrap)
 *
 * Notes:
 * - The player teleport disables the CharacterController first. It overrides
 *   transform writes, so without that the player silently stays at origin.
 */
```

The **Interacts With** section is the part that pays off — it tells someone what they'll break.

The **Notes** section is where anything that cost you an hour goes. That's the highest value part of the whole file.

**Style B — XML doc.** Families of small related classes, so IDE hover works. Currently the killstreaks.

```csharp
/// <summary>
/// COLD BOOT: immediately drops stress to zero and returns BPM to resting.
/// </summary>
```

**Style C — one-liner.** Small single-purpose files and data types.

```csharp
// hides whatever it is attached to when the game is built for webgl
```

## Inline comments

- Lowercase
- Above the line they explain, not beside it
- Explain **why**, not what

```csharp
// good, says why
// browsers eat escape to exit pointer lock, so p is the webgl fallback
if (Input.GetButtonDown("Cancel") || Input.GetKeyDown(KeyCode.P))

// bad, restates the code
// increment the kill counter
currentKills++;
```

**No author lines.** Git tracks that.

**No commented-out code.** Git remembers it. If a block is parked on purpose, say so in the header and why, so the next person doesn't assume it was forgotten.

**No `// TODO` without a name and a date.** Otherwise it's a wish.

**Never comment out a diagnostic.** Four `Debug.LogError` calls were silenced in `DoorController` and `WaveManager` while the `enabled = false; return;` beside them stayed. A door with no `doorObject` disabled itself with no message at all. If a log is too noisy, gate it behind a bool; don't delete the signal.

---

# 5. SERIALIZED FIELDS

**Every serialized field gets a `[Tooltip]`, written for a designer.** "bpm that kills the player, gdd says 200" beats "max bpm".

**Group them with `[Header]`.** A 34-field inspector with no headers is unusable.

**Renaming a serialized field destroys its saved value.** Every prefab and scene stores it under the old name. Unity finds no match, resets to the code default, and says nothing.

```csharp
using UnityEngine.Serialization;

[FormerlySerializedAs("damageStress")]
[SerializeField] private float stressPerHit = 20f;
```

**Mandatory on every rename.** Leave the attribute for one commit cycle, then remove it after every prefab and scene has been opened and re-saved.

This applies to serializable **types** too. Renaming a `[System.Serializable]` struct used as a field type loses values the same way, and `FormerlySerializedAs` doesn't cover type names — so those get renamed one at a time with an inspector check after each.

**`[SerializeField] public` is redundant.** Public fields serialize automatically.

---

# 6. TUNABLE VALUES

**Numbers from the GDD live in ScriptableObjects, not in code and not in prefabs.**

`maxBpm: 200` was serialized into nine separate prefab files. Changing it meant editing nine files, and any one could drift out of sync. Three code defaults were already disagreeing with their prefab values by the time it was found.

Current configs, in `Assets/Tunables/`:

```
StressConfig      bpm range, stress sources, decay
ScoreConfig       kill score formula, streak thresholds
EconomyConfig     Files per wave and for the boss
EnemyConfig       one asset per enemy type
```

**When a value stays a field instead:**

If something changes it at runtime, it has to be a field. `attackRate` is copied out of `EnemyConfig` in `Start` because `PacketLossKillstreak` multiplies it per enemy and restores it later. A ScriptableObject is shared by every instance, so mutating it would slow every enemy at once and corrupt the saved originals.

```csharp
private void Start()
{
    // config holds the authored value, this copy is what streaks and guns change
    attackRate = config.shotInterval;
}
```

**Scene object references never go in a config.** Those stay as serialized fields on the component.

---

# 7. EVENTS OVER DIRECT CALLS

If several systems care about something, raise an event. Don't call into each of them.

**Why.** `EnemyBase.Die()` used to call `GameManager`, `KillChainManager`, and `ChallengeManager` directly, each behind a null check. `ScoreManager.RegisterKill()` was never called at all — someone had to remember to add a line and didn't. The bug was invisible because a missing call looks like nothing.

```csharp
// EnemyBase.Die()
if (awardKillRewards)
    EnemyEvents.RaiseKilled(this);

// each consumer, in its own file
void OnEnable()  => EnemyEvents.Killed += handleKill;
void OnDisable() => EnemyEvents.Killed -= handleKill;
```

Adding a consumer now means writing a subscriber, not editing `EnemyBase`.

**Static events need clearing on play.** If Enter Play Mode Options has domain reload off, last session's destroyed subscribers stay attached.

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void clearOldEvents() { Killed = null; }
```

**Keep hard dependencies as direct calls.** The wave count can't advance until it knows an enemy died — that isn't a notification, it's a requirement. `waveHost.active.EnemyKilled()` stays a direct call.

---

# 8. SCENES AND PREFABS

**Bootstrap holds every manager.** It loads first and never unloads. Levels load additively on top.

**Level scenes hold geometry only.** No managers, no player, no camera, no UI. A level is a room, not a program.

Every level scene needs:
- The `LevelBootstrapper` prefab, so you can press play from it directly
- An object named `Player Spawn Pos`

**Never open a scene you don't own.** Unity dirties a scene just from opening it and moving the camera. That edit rides along in your commit and conflicts with the owner's work. Run `git status` before every push and un-stage anything you didn't mean to touch.

**Anything used in more than one place is a prefab.** The scene then stores a GUID and a transform instead of the whole object graph, so the scene file stops churning.

**All file moves happen in the Unity Project window.** Never Explorer. Moving a file outside Unity orphans its `.meta`, Unity regenerates a new GUID, and every reference to that asset breaks — showing up as conflicts on files nobody touched.

**Check references before deleting an asset.** `Untitled.fbx` looked like junk and was the boss arena platform mesh.

---

# 9. GIT

**Branch off main.** Name it `feat/<yourname>/<thing>` or `fix/<yourname>/<thing>`.

**Two days maximum.** If the work is bigger, split it. Land the plumbing first behind a disabled flag, the behavior second.

**Merge main into your branch every day you work.**

```bash
git fetch origin
git merge origin/main
```

Merge, don't rebase, for anything you've pushed. Conflict cost scales with how long branches sat apart, not with how much work was done.

**No personal branches.** `Samuel-CodeCreation` had no natural ending, so it never landed and collected a month of drift. That's how the repo reached 34 branches.

**No integration branches.** `mergebranch8` meant every change got merged twice and drifted further each time.

**Commit messages say what changed.** "Push for mass overhaul" doesn't help anyone find the bug in two weeks.

**One kind of change per commit.** Never mix a rename with a logic change — the result can't be reviewed or bisected.

## The merge tool — this is not optional

`.gitattributes` routes scenes, prefabs and `.meta` files through Unity's semantic merge tool. That only works if **each person** defines the driver in their own global git config. Config isn't versioned, so it's manual, per machine, once.

Without it, git falls back to merging YAML line by line, silently. That is why scenes kept breaking and why files got rebuilt from scratch.

```bash
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver "'<path>/UnityYAMLMerge.exe' merge -p --force --fallback none %O %B %A %A"
git config --global mergetool.unityyamlmerge.trustExitCode false
git config --global rerere.enabled true
```

Verify with `git config --get merge.unityyamlmerge.driver`. Empty means it isn't working.

## Case-only renames on Windows

Windows is case-insensitive and git hides it by default, so `gameManager.cs` → `GameManager.cs` looks like a no-op, commits nothing, and Unity writes the old name back on the next reimport.

```bash
git config core.ignorecase false     # once, per clone
git mv -f old.cs New.cs
git mv -f old.cs.meta New.cs.meta
```

**Unity must be closed** during case-only renames.

Turning off `ignorecase` also revealed four folder renames git had been hiding for days.

## Before running any script that edits files

**Close Unity and every editor.** An open IDE with the file buffered will re-save the old contents over your changes. `BossWaveManager.cs` was silently skipped by an entire pass this way.

---

# 10. FORMATTING

`.editorconfig` at the repo root drives this. Visual Studio and Rider both read it.

- 4 spaces, no tabs
- Allman braces — opening brace on its own line
- Braces on every `if`, even one-liners
- CRLF, UTF-8 with BOM
- `using` directives sorted, System first
- No runs of blank lines

Run **Analyze → Code Cleanup** before a large PR. Profile should include only: Format document, Sort usings, Remove unnecessary parentheses.

**Do not enable** "apply var preferences", "apply expression body preferences", or "add accessibility modifiers". Those are opinions, and letting a tool apply them across the project buries the real diff in noise.

**Do not run cleanup on `ThirdParty/`.** A package update overwrites it anyway.

---

# 11. THIRD PARTY

Everything downloaded lives under `Assets/ThirdParty/`. Never edit it — a package update replaces it wholesale, and your change goes with it.

**Delete demo folders on import.** Demo scenes, demo scripts and readmes are dead weight, and their scripts collide with yours. `PlayerController` collided with Yurowm's demo the moment team scripts moved to PascalCase.

---

# 12. CHECKS

Run these before a large PR.

```bash
# file name matches class name
for f in $(find Assets/Scripts -name "*.cs"); do
  n=$(basename "$f" .cs)
  grep -q "class $n\|interface $n\|struct $n\|enum $n" "$f" || echo "MISMATCH: $f"
done

# more than one top level type per file
for f in $(find Assets/Scripts -name "*.cs"); do
  c=$(grep -cE "^(public |internal |abstract |sealed )*(class|struct|interface|enum) " "$f")
  [ "$c" -gt 1 ] && echo "MULTI ($c): $f"
done

# field shadowing its own type
grep -rnE "\b([A-Z]\w+)\s+\1\s*[;=]" Assets/Scripts --include=*.cs | grep -vE "class |new |=>|\("

# duplicate tooltips
grep -rn "\[Tooltip" Assets/Scripts --include=*.cs | awk -F: \
  '{ if ($1==pf && $2==pl+1) print $1": line "$2; pf=$1; pl=$2 }'

# detached script references
grep -rc "m_Script: {fileID: 0}" Assets/Prefabs/ Assets/Scenes/ | grep -v ":0$"

# GDD numbers still hardcoded
grep -rn "= 200f\|= 100f\|= 20f\|= 3f\|= 70f" Assets/Scripts --include=*.cs

# methods wired to buttons — renaming any of these breaks it silently
grep -rh "m_MethodName:" Assets/Prefabs/ Assets/Scenes/ | sort -u
```

---

# 13. THINGS THAT BREAK SILENTLY

No error, no warning. Know these.

**Renaming a serialized field** resets its value in every prefab and scene.

**Renaming a public method wired to a Button.** UnityEvents store the name as a string. The inspector still looks connected and the button does nothing.

**Renaming a class referenced by a UnityEvent.** `m_TargetAssemblyTypeName` is also a string. Three were left pointing at pre-rename names.

**Two top-level types in one file.** Unity picks the wrong one and detaches components while "fixing" it.

**Case-only file renames on Windows.** Git records nothing, Unity reverts it.

**Deleting an asset with a meaningless name.** `Untitled.fbx` was the boss platform mesh.

**`?.` on a UnityEngine.Object.** Unity's fake-null means a destroyed object isn't `null` to `?.` but is to `==`. Use an explicit `!= null` check.

**A MonoBehaviour created with `new`.** Compiles, does nothing useful.

**An empty `Update()`.** Still costs a per-frame call on every instance.

---

# 14. OPEN ISSUES

Known, unfixed, recorded so nobody rediscovers them.

- **Files award compounds.** `WaveManager` and `GameManager` both add the running total rather than the amount earned, so the balance grows faster than intended.
- **Scorestreaks are unwired.** None of the ten components are attached to a prefab or scene, so the pool is empty and nothing can roll.
- **The shop is commented out** and still attached to live scenes.
- **The boss is 4 phases in code, 7 segments in the GDD.** One of the two is out of date.
- **22 unresolved asset GUIDs**, three in live prefabs — `AR`, `Audio Manager`, `Explosion`.
- **`BasicEnemy.attackDamage` is 20** against Heavy's 3 and Ranged's 1. Looks like a copy of `damageStress`.
- **Near miss detection is not implemented.** `HeartbeatManager.NearMiss()` has no callers and `nearMissStress` is tuned but unused.
