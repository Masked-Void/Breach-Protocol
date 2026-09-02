using UnityEngine;

public class randomUtility 
{

    /// <summary>
    /// Random true/false weighted by a 0-1 chance. Use instead of Random.Range so the
    /// int overload can't sneak in.
    /// </summary>
    /// <param name="chance">Probability of true, 0 never fires and 1 always fires.</param>
    /// <returns>True if the roll succeeded.</returns>
    public static bool roll(float chance) {

        if (chance <= 0) return false;
        if (chance >= 1) return true;

        bool output = Random.value < chance;

        return output;

    }

}
