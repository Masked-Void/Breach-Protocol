using System;
using System.Collections.Generic;

[Serializable]
public class serializationWrapper
{

    public List<string> keys = new List<string>();
    public List<int> progressVals = new List<int>();
    public List<bool> completedVals = new List<bool>();

    public serializationWrapper()
    {

    }
    public serializationWrapper(Dictionary<string, int> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
            keys.Add(keyValuePair.Key);
            progressVals.Add(keyValuePair.Value);
        }
    }

    public serializationWrapper(Dictionary<string, bool> dictionary)
    {
        foreach (var keyValuePair in dictionary)
        {
            keys.Add(keyValuePair.Key);
            completedVals.Add(keyValuePair.Value);
        }
    }

    public Dictionary<string, int> toProgDictionary()
    {
        Dictionary<string, int> targetDict = new Dictionary<string, int>();
        for (int i = 0; i < keys.Count; i++)
        {
            targetDict[keys[i]] = progressVals[i];
        }

        return targetDict;
    }

    public Dictionary<string, bool> toCompDictionary()
    {
        Dictionary<string, bool> targetDict = new Dictionary<string, bool>();
        for (int i = 0; i < keys.Count; i++)
        {
            targetDict[keys[i]] = completedVals[i];
        }

        return targetDict;
    }
}
