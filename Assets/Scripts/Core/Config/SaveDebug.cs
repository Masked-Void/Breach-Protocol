using UnityEngine;
//exists because [ContextMenu] can't live on a static class
public class SaveDebug : MonoBehaviour
{
    [Header("Debug Values")]
    [Tooltip("amount Grant Files adds to the balance, negative to subtract")]
    [SerializeField] private int filesToGrant = 100;

    [ContextMenu("Grant Files")]
    private void grantFiles()
    {
        SaveManager.Data.files += filesToGrant;
        SaveManager.MarkDirty();

        Debug.Log($"SaveManager: files is now {SaveManager.Data.files}");
    }
    [ContextMenu("Save Now")]
    private void saveNow()
    {
        SaveManager.Save();
        Debug.Log($"SaveManager: wrote {Application.persistentDataPath}");
    }

    [ContextMenu("Reload from Disk")]
    private void loadNow()
    {
        SaveManager.Load();
        Debug.Log($"SaveManager: reloaded from {Application.persistentDataPath}");
    }

    [ContextMenu("Wipe Save -- Deletes all progress")]
    private void resetNow()
    {
        SaveManager.ResetSave();
        Debug.LogWarning($"SaveManager: wiped all progress at {Application.persistentDataPath}");
    }

    [ContextMenu("Log Save Path")]
    private void logSavePath()
    {
        Debug.Log($"SaveManager: save path is {Application.persistentDataPath}");
    }
}
