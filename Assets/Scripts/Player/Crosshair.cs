using UnityEngine;
using UnityEngine.UI;

/*
 * Script: Crosshair
 *
 * Description:
 * Four-piece crosshair that spreads apart when the player fires and eases back
 * in. Also recolours when aiming at something worth highlighting.
 *
 * Interacts With:
 * - WeaponManager (calls Expand on fire)
 * - PlayerInteraction (calls SetTarget when looking at a pickup)
 */
public class Crosshair : MonoBehaviour
{
    [Header("Spread")]
    [Tooltip("current spread in pixels, set at runtime by Expand, eases back to 0")]
    [Range(0, 25)]
    public float value;

    [Tooltip("how fast the pieces move toward their target position")]
    public float speed;

    [Tooltip("gap from centre even at zero spread, so the pieces never overlap")]
    public float margin;

    [Header("Pieces")]
    [Tooltip("the four arms and the centre dot, positioned each frame")]
    public RectTransform top, bottom, left, right, center;

    [Tooltip("images on those same pieces, recoloured by SetTarget")]
    public Image topImage, bottomImage, leftImage, rightImage, centerImage;

    [Tooltip("colour used when not aiming at anything special")]
    public Color normalColor = Color.white;
    void Update()
    {
        float topValue = Mathf.Lerp(top.position.y,
            center.position.y + margin + value,
            speed * Time.deltaTime);

        float bottomValue = Mathf.Lerp(bottom.position.y,
            center.position.y - margin - value,
            speed * Time.deltaTime);

        float leftValue = Mathf.Lerp(left.position.x,
            center.position.x - margin - value,
            speed * Time.deltaTime);

        float rightValue = Mathf.Lerp(right.position.x,
            center.position.x + margin + value,
            speed * Time.deltaTime);

        top.position = new Vector2(top.position.x, topValue);
        bottom.position = new Vector2(bottom.position.x, bottomValue);

        left.position = new Vector2(leftValue, center.position.y);
        right.position = new Vector2(rightValue, center.position.y);
    }

    // opens the crosshair to a given spread, it eases back in on its own
    public void Expand(float amount)
    {
        value = amount;
    }

    // recolours the four arms, pass false to go back to normalColor
    public void SetTarget(bool change, Color targetColor)
    {
        Color c = change ? targetColor : normalColor;

        topImage.color = c;
        bottomImage.color = c;
        leftImage.color = c;
        rightImage.color = c;
    }
}