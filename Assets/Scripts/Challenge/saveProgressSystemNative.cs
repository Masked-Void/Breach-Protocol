using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class saveProgressSystemNative
{
    public Dictionary<string, int> progressDict = new Dictionary<string, int>();
    private string path => Path.Combine(Application.persistentDataPath, "challenge_progress");
    public void saveWithJsonUtility()
    {
        

        serializationWrapper wrapper = new serializationWrapper(progressDict);

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
      progressDict = wrapper.toProgDictionary();
        
    }
}