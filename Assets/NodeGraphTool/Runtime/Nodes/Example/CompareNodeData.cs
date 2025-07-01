using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using NodeGraph;


public enum Compare
{ 
    Less,
    LessOrEqual,
    Equal,
    GreaterOrEqual,
    Greater,
    NotEqual

}

public class CompareNodeData : NodeBaseData
{
    //  private bool result;
  //  public string description;
    public Compare compare;

    public bool GetResult(float a, float b)
    { 
         bool result = false;
        switch (compare)
        {
            case Compare.Less:
                if (a < b) result = true;
                break;
            case Compare.LessOrEqual:
                if (a <= b) result = true;
                break;
            case Compare.Equal:
                if (a == b) result = true;
                break;
            case Compare.GreaterOrEqual:
                if (a >=b) result = true;
                break;
            case Compare.Greater:
                if (a > b) result = true;
                break;
            case Compare.NotEqual:
                if (a != b) result = true;
                break;
        }
        return result;
    }
    public bool GetResult(int a, int b)
    {
        bool result = false;
        switch (compare)
        {
            case Compare.Less:
                if (a < b) result = true;
                break;
            case Compare.LessOrEqual:
                if (a <= b) result = true;
                break;
            case Compare.Equal:
                if (a == b) result = true;
                break;
            case Compare.GreaterOrEqual:
                if (a >= b) result = true;
                break;
            case Compare.Greater:
                if (a > b) result = true;
                break;
            case Compare.NotEqual:
                if (a != b) result = true;
                break;
        }
        return result;
    }
    public bool GetResult(Vector2 a, Vector2 b)
    {
        bool result = false;
        switch (compare)
        {
            case Compare.Less:
                if (Vector2.SqrMagnitude(a) < Vector2.SqrMagnitude(b)) result = true;
                break;
            case Compare.LessOrEqual:
                if (Vector2.SqrMagnitude(a) <= Vector2.SqrMagnitude(b)) result = true;
                break;
            case Compare.Equal:
                if (Vector2.SqrMagnitude(a) == Vector2.SqrMagnitude(b)) result = true;
                break;
            case Compare.GreaterOrEqual:
                if (Vector2.SqrMagnitude(a) >= Vector2.SqrMagnitude(b)) result = true;
                break;
            case Compare.Greater:
                if (Vector2.SqrMagnitude(a) > Vector2.SqrMagnitude(b)) result = true;
                break;
            case Compare.NotEqual:
                if (Vector2.SqrMagnitude(a) != Vector2.SqrMagnitude(b)) result = true;
                break;
        }
        return result;
    }
    public bool GetResult(Vector3 a, Vector3 b)
    {
        bool result = false;
        switch (compare)
        {
            case Compare.Less:
                if (Vector3.SqrMagnitude(a) < Vector3.SqrMagnitude(b)) result = true;
                break;
            case Compare.LessOrEqual:
                if (Vector3.SqrMagnitude(a) <= Vector3.SqrMagnitude(b)) result = true;
                break;
            case Compare.Equal:
                if (Vector3.SqrMagnitude(a) == Vector3.SqrMagnitude(b)) result = true;
                break;
            case Compare.GreaterOrEqual:
                if (Vector3.SqrMagnitude(a) >= Vector3.SqrMagnitude(b)) result = true;
                break;
            case Compare.Greater:
                if (Vector3.SqrMagnitude(a) > Vector3.SqrMagnitude(b)) result = true;
                break;
            case Compare.NotEqual:
                if (Vector3.SqrMagnitude(a) != Vector3.SqrMagnitude(b)) result = true;
                break;
        }
        return result;
    }
    public bool GetResult(Vector4 a, Vector4 b)
    {
        bool result = false;
        switch (compare)
        {
            case Compare.Less:
                if (Vector4.SqrMagnitude(a) < Vector4.SqrMagnitude(b)) result = true;
                break;
            case Compare.LessOrEqual:
                if (Vector4.SqrMagnitude(a) <= Vector4.SqrMagnitude(b)) result = true;
                break;
            case Compare.Equal:
                if (Vector4.SqrMagnitude(a) == Vector4.SqrMagnitude(b)) result = true;
                break;
            case Compare.GreaterOrEqual:
                if (Vector4.SqrMagnitude(a) >= Vector4.SqrMagnitude(b)) result = true;
                break;
            case Compare.Greater:
                if (Vector4.SqrMagnitude(a) > Vector4.SqrMagnitude(b)) result = true;
                break;
            case Compare.NotEqual:
                if (Vector4.SqrMagnitude(a) != Vector4.SqrMagnitude(b)) result = true;
                break;
        }
        return result;
    }
}
