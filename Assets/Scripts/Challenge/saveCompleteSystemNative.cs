using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class saveCompleteSystemNative
{
    public Dictionary<string, bool> completeDict = new Dictionary<string, bool>();
    private string path => Path.Combine(Application.persistentDataPath, "challenge_complete");
    public void saveWithJsonUtility()
    {
        serializationWrapper wrapper = new serializationWrapper(completeDict);

        string json = JsonUtility.ToJson(wrapper, true);

        File.WriteAllText(path, json);
    }

    public void loadWithJsonUtility()
    {
        if (!File.Exists(path))
        {
            return;
        }

    string json = File.ReadAllText(path);

    serializationWrapper wrapper = JsonUtility.FromJson<serializationWrapper>(json);
        if (wrapper == null)
        {
            return;
        }
        completeDict = wrapper.toCompDictionary();
        }
    }

