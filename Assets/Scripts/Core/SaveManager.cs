using UnityEngine;
using System.IO;
using System;

// holds the player's saved progress and reads/writes it to a json file in persistentDataPath.
// lives in Bootstrap and survives scene loads.
public class SaveManager : MonoBehaviour
{

    public static SaveManager instance;

    [Header("Save File")]
    [Tooltip("File name written to Application.perstentDataPath.")]
    [SerializeField] private string fileName = "save.json";

    [Header("Load Data")]
    [Tooltip("Filled in on Awake from the file on disk, Read only, dont hand edit this")]
    [SerializeField] private SaveData data = new SaveData();

    public SaveData Data => data;
    public bool IsDirty => isDirty;

    private string filePath;
    private bool isDirty;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        SaveIfDirty();
    }

    // Reads the save off the disk. Falls back to a fresh save if file is missing or unreadable
    [ContextMenu("Load")]
    public void Load()
    {

        ensurePath();

        if (!File.Exists(filePath))
        {
            data = new SaveData();
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch (Exception e)
        {
            // For corrupt or half written file
            Debug.LogError($"SaveManager: Couldn't read {filePath}, starting fresh. {e.Message}", this);
            data = null;
        }

        if (data == null)
        {
            data = new SaveData();
        }
    }

    // writes whatever is currently in data out to disk.
    [ContextMenu("Save")]
    public void Save()
    {
        ensurePath();

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            isDirty = false;
        }
        catch (Exception e)
        {
            // Disk full, locked file, or bad permissions
            Debug.LogError($"SaveManager: couldn't write {filePath}. {e.Message}", this);
        }
    }

    public void SaveIfDirty()
    {
        if (isDirty)
        {
            Save();
        }
    }

    // Screw you mark, kidding
    // Marks the file as needing a write, use if you change something in the data directly
    public void MarkDirty()
    {
        isDirty = true;
    }

    // wipes all progress and writes the empty save straight away.
    // not called Reset because MonoBehaviour.Reset is a unity message the editor fires on its own.
    [ContextMenu("Reset Save")]
    public void ResetSave()
    {
        data = new SaveData();
        Save();
    }

    // builds the path if it hasn't been built yet, so the context menu buttons work in edit mode too
    private void ensurePath()
    {
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
