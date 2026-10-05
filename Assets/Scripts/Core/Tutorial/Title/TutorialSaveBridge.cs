// the only tutorial code that touches the save, so the flags live in one place. SaveManager is static, so this is too
public class TutorialSaveBridge
{
    // true once this save has been asked about the tutorial, either answer counts
    public static bool HasPlayedBefore => SaveManager.Data.hasPlayedBefore;

    // true once the tutorial was finished rather than skipped, so a finish the tutorial challenge can check it
    public static bool HasCompletedTutorial => SaveManager.Data.hasCompletedTutorial;

    public static void MarkPlayed()
    {
        if (SaveManager.Data.hasPlayedBefore)
        {
            return;
        }

        SaveManager.Data.hasPlayedBefore = true;

        // written now, a crash before quitting would otherwise ask again next launch
        SaveManager.Save();
    }

    public static void MarkTutorialComplete()
    {
        if (SaveManager.Data.hasCompletedTutorial)
        {
            return;
        }

        SaveManager.Data.hasCompletedTutorial = true;
        SaveManager.Save();
    }
}
