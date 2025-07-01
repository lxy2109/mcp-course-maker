using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class MeshExtruder
{
    public static Mesh Extrude(float3[] path, float3[] shape, float[] shapeUV, bool fixedTotalV,float uvFactor, int subdivition = 0)
    {
        //build points
        List<Vector3> points = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        float3 curAnchor;
        float3 curTangent;
        Quaternion rotationOffset;
        float prevV = 0;
        float3 prevAnchor = path[0];
        float stepT = 1f / path.Length;

        for (int i = 0; i < path.Length - 1; i++)
        {
            curAnchor = path[i];
            curTangent = path[i + 1] - curAnchor;
            if(((Vector3)curTangent).magnitude > 0)
            {
                rotationOffset = Quaternion.FromToRotation(Vector3.up, curTangent);
            }
            else
            {
                rotationOffset = Quaternion.identity;
            }

            for(int sp = 0; sp < shape.Length; sp++)
            {
                float3 curPointOffset = rotationOffset * shape[sp];
                float3 curPoint = curAnchor + curPointOffset;
                points.Add(curPoint);
                normals.Add(curPoint - curAnchor);
                float u = shapeUV[sp];
                float v;
                if (fixedTotalV)
                {
                    v = stepT * i*uvFactor;
                }
                else
                {
                    v = prevV + ((Vector3)(curAnchor - prevAnchor)).magnitude * uvFactor;
                }
                uv.Add(new Vector2(u, v));
                prevV = v;
                prevAnchor = curAnchor;
            }
        }

        curAnchor = path[path.Length - 1];
        curTangent = curAnchor - path[path.Length - 2] ;
        rotationOffset = Quaternion.FromToRotation(Vector3.up, curTangent);

        for (int sp = 0; sp < shape.Length; sp++)
        {
            float3 curPointOffset = rotationOffset * shape[sp];
            float3 curPoint = curAnchor + curPointOffset;
            points.Add(curPoint);
            normals.Add(curPoint - curAnchor);
            float u = shapeUV[sp];
            float v;
            if (fixedTotalV)
            {
                v = 1*uvFactor;
            }
            else
            {
                v = prevV + ((Vector3)(curAnchor - prevAnchor)).magnitude * uvFactor;
            }
            uv.Add(new Vector2(u, v));
        }

        //build triangles
        List<int> triangles = new List<int>();
        int seg = shape.Length;
        for(int x = 0; x < path.Length - 1; x++)
        {
            for(int y = 0; y < shape.Length - 1; y++)
            {
                //make a quad for (x,y) (x + 1,y) (x + 1, y + 1) (x, y + 1)
                int indexA = y + seg * x;
                int indexB = indexA + 1;
                int indexC = indexB + seg;
                int indexD = indexA + seg;

                triangles.Add(indexA);
                triangles.Add(indexB);
                triangles.Add(indexC);
                        
                triangles.Add(indexA);
                triangles.Add(indexC);
                triangles.Add(indexD);
            }
        }
        Mesh result = new Mesh();
        result.SetVertices(points);
        result.triangles = triangles.ToArray();
        result.normals = normals.ToArray();
        result.uv = uv.ToArray();
        result.name = "temp";
        return result;

    }
}
