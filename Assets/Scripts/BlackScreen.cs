using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//创建一个全屏图片，选择一个Camera，然后移动它就可以看到效果
public class BlackScreen : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;//创建一个全屏图片，选择一个Camera，然后移动它就可以看到效果
    public AnimationCurve curve; //在Inspector中添加一个AnimationCurve
    [Range(0.5f, 2f)] 
    public float speed = 1f; //设置颜色渐变速度

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //自动开始
    private void OnEnable()
    {
        StartCoroutine(Black());
    }

    Color tmpColor; //当前颜色
    public IEnumerator Black()
    {
        float timer = 0f;
        tmpColor = spriteRenderer.color;
        do
        {
            timer += Time.deltaTime;
            SetColorAlpha(curve.Evaluate(timer * speed));
            yield return null;

        } while (tmpColor.a > 0);
        gameObject.SetActive(false);
    }

    //通过图片渐变实现颜色渐变
    void SetColorAlpha(float a)
    {
        tmpColor.a = a;
        spriteRenderer.color = tmpColor;
    }
}