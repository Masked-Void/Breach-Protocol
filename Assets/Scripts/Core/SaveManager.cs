using UnityEngine;
using System.IO;
using System;
/*
 * Script: SaveManager
 *
 * Description:
 * Owns the single save.json file in persistentDataPath. Static, with no scene
 * presence — the first read of Data loads the file, so nothing has to run first.
 *
 * Responsibilities:
 * - Read and write the save file, falling back to a fresh save on a bad read
 * - Track whether memory and disk have diverged, via MarkDirty and SaveIfDirty
 * - Clear its statics each play, and write once on quit
 *
 * Interacts With:
 * - SaveData (the object it serializes)
 * - ChallengeManager, UpgradeManager, AudioManager, WeaponManager (all read Data)
 * - SaveDebug (context menu wrapper, since a static class cannot carry one)
 *
 * Notes:
 * - Static rather than a MonoBehaviour because those managers all read the save
 *   in Awake, and Unity does not order Awake between objects. An instance version
 *   is null for whichever one runs first.
 * - Application.quitting does not fire on a crash, so SaveIfDirty still needs
 *   calling at round end and on death.
 */

// holds the player's saved progress and reads/writes it to a json file in persistentDataPath.
// lives in Bootstrap and survives scene loads.
public static class SaveManager 
{
    private const string FileName = "save.json";
    private static SaveData data;
    private static bool isDirty;
    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void init()
    {
        data = null;
        isDirty = false;

        Application.quitting -= SaveIfDirty;
        Application.quitting += SaveIfDirty;
    }
    // Reads the save off the disk. Falls back to a fresh save if file is missing or unreadable
    public static SaveData Data
    {
        get
        {
            ensureLoaded();
            return data;
        }
    }
    public static void Load()
    {
        if (!File.Exists(FilePath))
        {
            data = new SaveData();
        }
        else
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                // for a corrupt or half written file
                Debug.LogError($"SaveManager: couldn't read {FilePath}, starting fresh. {e.Message}");
                data = null;
            }
        }

        // FromJson returns null on an empty or malformed file without throwing
        if (data == null)
        {
            data = new SaveData();
        }

        data.FixNulls();
        isDirty = false;
    }

    // writes whatever is currently in data out to disk.
    public static void Save()
    {
        ensureLoaded();
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FilePath, json);
            isDirty = false;
        }
        catch (Exception e)
        {
            // Disk full, locked file, or bad permissions
            Debug.LogError($"SaveManager: couldn't write {FilePath}. {e.Message}");
        }
    }

    public static void SaveIfDirty()
    {
        // nothing can have set isDirty without the save already being loaded, so
        // checking the flag first avoids a pointless disk read on a quiet quit
        if (!isDirty)
        {
            return;
        }

        Save();
    }

    // Screw you mark, kidding
    // Marks the file as needing a write, use if you change something in the data directly
    public static void MarkDirty()
    {
        ensureLoaded();
        isDirty = true;
    }

    // wipes all progress and writes the empty save straight away.
    // not called Reset because MonoBehaviour.Reset is a unity message the editor fires on its own.
    public static void ResetSave()
    {
        ensureLoaded();
        data = new SaveData();
        Save();
    }

    private static void ensureLoaded()
    {
        if (data == null)
        {
            Load();
        }
    }
}
