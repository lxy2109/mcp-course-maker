using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ValueAnimation 
{
    public static float Lerp(float a, float b, float mid)
    {
        a = Mathf.Lerp(a, b, Time.deltaTime*mid);
        if (Mathf.Abs(a - b) <= 0.01f) { a = b; }
        return a;
    }

    public static Vector3 Lerp(Vector3 a, Vector3 b, float mid)
    {
        a = Vector3.Lerp(a, b, Time.deltaTime * mid);
        if (Vector3.Distance(a, b) <= 0.01f) { a = b; }
        return a;
    }

    public static Vector4 Lerp(Vector4 a, Vector4 b, float mid)
    {
        a = Vector4.Lerp(a, b, Time.deltaTime * mid);
        if (Vector4.Distance(a, b) <= 0.01f) { a = b; }
        return a;
    }
    public static Color Lerp(Color a, Color b, float mid)
    {
        a = Vector4.Lerp(a, b, Time.deltaTime * mid);
        if (Vector4.Distance(a, b) <= 0.01f) { a = b; }
        return a;
    }
}
