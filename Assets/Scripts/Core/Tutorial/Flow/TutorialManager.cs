using UnityEngine;
using UnityEngine.Events;

/*
 * Script: TutorialManager
 *
 * Description:
 * Runs the concentric tutorial from the inside out. Each space's lesson has to
 * finish before its boundary starts dissolving, and the boundary still needs the
 * player moving to open, because walls dissolve on scaled time.
 *
 * Interacts With:
 * - TutorialLesson (every space's lesson registers here)
 * - RingBoundary (one per space, cell first)
 * - TutorialServices (tutorial mode and the adapters to the real game)
 * - TutorialExit (saving and loading the title), TutorialSkip (skip and debug keys)
 * - GameManager (PlayerReady and the player)
 *
 * Notes:
 * - Pressing play from this scene means the Bootstrap player doesn't exist during
 *   Start, so beginning also listens for GameManager.PlayerReady.
 * - A duplicate destroys only its component, since the services, exit and hint
 *   system share this object.
 */

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;
}
