using System;

// one challenge's saved state. keeps the key, its progress and whether it's done
// together in one object so they can't fall out of sync.
[Serializable]
public class ChallengeEntry
{
    public string key;
    public int progress;
    public bool complete;

    // needed by JsonUtility, don't delete
    public ChallengeEntry()
    {
        key = string.Empty;
        progress = 0;
        complete = false;
    }

    public ChallengeEntry(string key, int progress = 0, bool complete = false)
    {
        this.key = key;
        this.progress = progress;
        this.complete = complete;
    }
}
