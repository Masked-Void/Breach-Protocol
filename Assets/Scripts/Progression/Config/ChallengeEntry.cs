using System;

// one challenge's saved state. keeps the id, its progress and whether it's done
// together in one object so they can't fall out of sync.
[Serializable]
public class ChallengeEntry
{
    public string id;
    public int progress;
    public bool complete;

    // needed by JsonUtility, don't delete
    public ChallengeEntry()
    {
        id = string.Empty;
        progress = 0;
        complete = false;
    }

    public ChallengeEntry(string id, int progress = 0, bool complete = false)
    {
        this.id = id;
        this.progress = progress;
        this.complete = complete;
    }
}
