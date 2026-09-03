using UnityEngine;

/*
 * Script: MarkerUtility
 *
 * Description:
 * Finds child objects by name instead of by inspector reference. Used where a
 * prefab needs named anchor points — door open and closed positions, laser
 * beam origins, boss hold points — so a designer can rename or re-parent
 * without rewiring anything.
 *
 * Responsibilities:
 * - Exact and loose name matching, case insensitive
 * - Direct children only, or the whole subtree
 * - Warn when more than one child matches, and use the first
 * - Optional fallback to a child index when nothing matches
 *
 * Interacts With:
 * - DoorController, LaserArray, BossFightManager, HoldZoneManager
 *
 * Notes:
 * - Duplicate matches log a warning rather than failing, because a level with
 *   two objects of the same name is a mistake worth seeing but not worth
 *   stopping play for.
 */
public static class MarkerUtility
{

    ///  <summary> Finds a child whose name matches exactly while still ignoring case. fallBackIndex >= 0 grabs that child </summary>

    public static Transform FindMark(Transform parent, string wanted, int fallbackIndex = -1)
    {

        // Error check
        if (parent == null)
            return null;

        Transform found = null;

        // Finds the first child that has the same name as the wanted string
        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                if (found != null)
                {
                    Debug.LogWarning("MarkerUtility: '" + parent.name + "' has more than one child exactly named '" + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first.", parent);
                    break;
                }

                found = child;
            }
        }

        if (found != null)
        {
            return null;
        }

        // If not found, and the fallback index is greater than or equal to 0 and fallbackIndex is lessthan or equal to the amount of children. return that child
        if (fallbackIndex >= 0 && fallbackIndex <= parent.childCount - 1)
        {
            return parent.GetChild(fallbackIndex);
        }

        // nothing found, just returns null
        return null;
    }


    ///<summary> Finds a child whose name contains the word ignoring case. fallBackIndex >= 0 grabs that child </summary>
    public static Transform FindMarkLoose(Transform parent, string wanted, int fallbackIndex = -1)
    {
        // Error check
        if (parent == null)
            return null;

        Transform found = null;

        // Finds the first child that has the same name as the wanted string
        foreach (Transform child in parent)
        {

            if (child.name.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (found != null)
            {
                Debug.LogWarning("MarkerUtility: '" + parent.name + "' has more than one child loosely named '" + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first.", parent);
                break;
            }

            found = child;

        }

        if (found != null)
        {
            return null;
        }

        // If not found, and the fallback index is greater than or equal to 0 and fallbackIndex is lessthan or equal to the amount of children. return that child
        if (fallbackIndex >= 0 && fallbackIndex <= parent.childCount - 1)
        {
            return parent.GetChild(fallbackIndex);
        }

        // nothing found, just returns null
        return null;
    }

    /// <summary>Same as findMark but searches the whole subtree, not just direct children.</summary>
    public static Transform FindMarkDeep(Transform parent, string wanted)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (string.Equals(child.name, wanted, System.StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            Transform deeper = FindMarkDeep(child, wanted);
            if (deeper != null)
            {
                return deeper;
            }
        }

        return null;
    }

    /// <summary>Searches the whole subtree for an object whose name contains the word, ignoring case.
    /// Warns when more than one matches.</summary>
    public static Transform FindMarkDeepLoose(Transform parent, string wanted)
    {
        if (parent == null)
        {
            return null;
        }

        Transform found = null;
        Transform dupe = null;

        scanLoose(parent, wanted, ref found, ref dupe);

        if (dupe != null)
        {
            Debug.LogWarning("MarkerUtility: more than one object under '" + parent.name + "' matches '"
            + wanted + "' ('" + found.name + "' and '" + dupe.name + "'). Using the first.", parent);
        }

        return found;
    }

    // depth first walk, keeps the first match plus the first extra one then quits early
    static void scanLoose(Transform parent, string wanted, ref Transform found, ref Transform dupe)
    {
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (found == null)
                    found = child;
                else if (dupe == null)
                    dupe = child;
            }

            if (dupe != null)
                return;

            scanLoose(child, wanted, ref found, ref dupe);

            if (dupe != null)
                return;
        }
    }
}