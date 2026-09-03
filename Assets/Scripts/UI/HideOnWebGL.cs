using UnityEngine;

// hides whatever it is attached to when the game is built for webgl
// browsers cannot close their own tab so exit buttons do nothing there
public class HideOnWebGL : MonoBehaviour
{
    private void Awake()
    {
        // editor keeps showing it so the menus still look right while working
#if UNITY_WEBGL && !UNITY_EDITOR
            gameObject.SetActive(false);
#endif
    }
}
