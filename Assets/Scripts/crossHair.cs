using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Range(0, 25)]
    public float value;

    public float speed;
    public float margin;

    LayerMask interactLayer;

    public RectTransform top, bottom, left, right, center;
    public Image topImage, bottomImage, leftImage, rightImage, centerImage;

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

    public void expand(float amount)
    {
        value = amount;
    }

    public void setTarget(bool change, Color targetColor)
    {
        Color c = change ? targetColor : normalColor;

        topImage.color = c;
        bottomImage.color = c;
        leftImage.color = c;
        rightImage.color = c;
    }
}