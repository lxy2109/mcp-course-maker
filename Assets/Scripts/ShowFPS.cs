using UnityEngine;
using TMPro;

public class ShowFPS : MonoBehaviour
{
    public float updateInterval = 0.5f;
    public TMP_Text FPSText;

    private float accum = 0.0f; // 累计时间
    private int frames = 0; // 帧数
    private float timeLeft; // 剩余时间

    private void Start()
    {
        timeLeft = updateInterval;
    }

    private void Update()
    {
        timeLeft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        frames++;

        // 每秒计算一次帧数
        if (timeLeft <= 0.0f)
        {
            float fps = accum / frames;
            string fpsText = string.Format("{0:F2} FPS", fps);
            FPSText.text = fpsText;

            timeLeft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}