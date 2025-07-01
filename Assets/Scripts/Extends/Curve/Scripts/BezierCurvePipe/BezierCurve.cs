using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class BezierCurve
{
    public float3[] points = new float3[4];
    public float3 p0
    {
        get
        {
            return  points[0];
        }
        set
        {
                    points[0] = value;
        }
    }
    public float3 p1
    {
        get
        {
            return points[1];
        }
        set
        {
            points[1] = value;
        }
    }
    public float3 p2
    {
        get
        {
            return points[2];
        }
        set
        {
            points[2] = value;
        }
    }
    public float3 p3
    {
        get
        {
            return points[3];
        }
        set
        {
            points[3] = value;
        }
    }

    public float3 Sample(float t)
    {
        float remain = 1 - t;
        float remain2 = remain * remain;
        float remain3 = remain2 * remain;

        return remain3 * p0 + (3 * remain2 * t * p1) + (3 * remain * t * t * p2) + t * t * t * p3;
    }
    public float3 GetTangent(float t)
    {
        return -3  * Mathf.Pow(1 - t,2) * p0 + 3 * Mathf.Pow((1 - t),2) * p1 - 6 * t *(1 - t) * p1 - 3 * t * t * p2 + 6 * t *(1 - t) * p2 + 3* t * t * p3;
    }
    public float3[] GetSampledArray(int resolution)
    {
        List<float3> resultList = new List<float3>();
        float stepLength = 1f / resolution;
        float curT = 0;
        while(curT < 1)
        {
            float3 curPoint = Sample(curT);
            resultList.Add(curPoint);
            curT += stepLength;
        }
        resultList.Add(p3);
        return resultList.ToArray();
    }
    public Vector3[] GetSampledVector3Array(int resolution)
    {
        List<Vector3> resultList = new List<Vector3>();
        float stepLength = 1f / resolution;
        float curT = 0;
        while (curT < 1)
        {
            float3 curPoint = Sample(curT);
            resultList.Add(curPoint);
            curT += stepLength;
        }
        resultList.Add(p3);
        return resultList.ToArray();
    }
    public float3[] GetSampledTangetArray(int resolution)
    {
        List<float3> resultList = new List<float3>();
        float stepLength = 1f / resolution;
        float curT = 0;
        while (curT < 1)
        {
            float3 curPoint = GetTangent(curT);
            resultList.Add(curPoint);
            curT += stepLength;
        }
        resultList.Add(p3);
        return resultList.ToArray();
    }

    public void SetPoints(float3 p0, float3 p1, float3 p2, float3 p3)
    {
        this.p0 = p0;
        this.p1 = p1;
        this.p2 = p2;
        this.p3 = p3;
        return;
    }

    public void DrawGizmos(int resolution)
    {
        float3[] points = GetSampledArray(resolution);
        float3[] tangents = GetSampledTangetArray(resolution);
        for(int i = 0; i < points.Length - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
        }
        return;
    }

    public BezierCurve()
    {

    }
}
