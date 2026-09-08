using TMPro;
using UnityEngine;

public class TimescaleUI : MonoBehaviour
{
    [SerializeField] TMP_Text timescale;

    private void Update()
    {
        timescale.text = (TimeManager.instance.TimeScale).ToString();
    }
}
