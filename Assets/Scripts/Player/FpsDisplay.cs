using UnityEngine;
using TMPro;

public class FpsDisplay : MonoBehaviour
{
    public TMP_Text fpsText;
    public float updateInterval = 0.5f;

    private float accumulatedTime = 0f;
    private int frames = 0;

    void Update()
    {
        accumulatedTime += Time.unscaledDeltaTime;
        frames++;

        if (accumulatedTime >= updateInterval)
        {
            float fps = frames / accumulatedTime;
            fpsText.text = $"{fps:0.} FPS";

            frames = 0;
            accumulatedTime = 0f;
        }
    }
}
