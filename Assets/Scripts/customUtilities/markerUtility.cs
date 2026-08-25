using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public static class markerUtility {

    ///  <summary> Finds a child whose name matches exactly while still ignoring case. fallBackIndex >= 0 grabs that child </summary>
    
    public static Transform findMark(Transform parent, string wanted, int fallbackIndex = -1) {

        // Error check
        if (parent == null) return null;

        Transform found = null;

        // Finds the first child that has the same name as the wanted string
        foreach(Transform child in parent) {
            if (string.Equals(child.name , wanted , System.StringComparison.OrdinalIgnoreCase)) {
                if (found != null) {
                    Debug.LogWarning("markerUtility: '" + parent.name + "' has more than one child exactly named '" + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first." , parent);
                    break;
                }

                found = child;
            }
        }

        if (found != null) {
            return null;
        }

        // If not found, and the fallback index is greater than or equal to 0 and fallbackIndex is lessthan or equal to the amount of children. return that child
        if (fallbackIndex >= 0 && fallbackIndex <= parent.childCount - 1) {
            return parent.GetChild(fallbackIndex);
        }

        // nothing found, just returns null
        return null;
    }


    ///<summary> Finds a child whose name contains the word ignoring case. fallBackIndex >= 0 grabs that child </summary>
    public static Transform findMarkLoose(Transform parent, string wanted, int fallbackIndex = -1) {
        // Error check
        if (parent == null)
            return null;

        Transform found = null;

        // Finds the first child that has the same name as the wanted string
        foreach (Transform child in parent) {
            
            if (child.name.IndexOf(wanted , System.StringComparison.OrdinalIgnoreCase) < 0)
                continue; 
            
            if (found != null) {
                Debug.LogWarning("markerUtility: '" + parent.name + "' has more than one child loosely named '" + wanted + "' ('" + found.name + "' and '" + child.name + "'). Using the first." , parent);
                break;
            }

            found = child;
            
        }

        if (found != null) {
            return null;
        }

        // If not found, and the fallback index is greater than or equal to 0 and fallbackIndex is lessthan or equal to the amount of children. return that child
        if (fallbackIndex >= 0 && fallbackIndex <= parent.childCount - 1) {
            return parent.GetChild(fallbackIndex);
        }

        // nothing found, just returns null
        return null;
    }
}