using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
[ExecuteInEditMode]
public class BezierPipeRenderer : MonoBehaviour
{
    [Range(1,64)]
    public int resolution;
    public int verticalSegments;
    public float radius;
    public float vFactor;
    public bool fixedTotalV;
    public float3[] GetShapeArray()
    {
        List<float3> result = new List<float3>();
        float eulerStep = 360f / verticalSegments;
        float3 up = new float3(0, 0, 1);
        for(int i = 0; i < verticalSegments; i++)
        {
            result.Add( Quaternion.Euler(0, eulerStep * i, 0) * up * radius);
        }
        result.Add(result[0]);
        return result.ToArray();
    }
    public float[] GetShapeU()
    {
        float stepLength = 1f / verticalSegments;
        List<float> result = new List<float>();
        for(int i = 0; i < verticalSegments; i++)
        {
            result.Add(stepLength * i);
        }
        result.Add(1);
        return result.ToArray();
    }
    public MeshFilter meshFilter;
    public BezierCurve bezierCurve = new BezierCurve();
    private MeshFilter previewMeshFilter;
    public ConnectionRenderer connection;
    public bool preview;

    private Mesh tempMesh;

    [Header("Default Material")]
    public Material material;
    public float3[] path
    {
        get
        {
            return bezierCurve.GetSampledArray(resolution);
        }
    }
    public float[] u
    {
        get
        {
            return GetShapeU();
        }
    }
    // Update is called once per frame


    void Update()
    {
        //if (!Application.isPlaying)
        //{
        //    PreviewMesh();
        //}
        PreviewMesh();
    }



    public GameObject CreatMesh()
    {
        connection.UpdateCruves();
        GameObject temp =new GameObject("pipe");
        //temp.AddComponent<Transform>();
        temp.AddComponent<MeshFilter>();
        temp.AddComponent<MeshRenderer>();
        temp.AddComponent<MeshCollider>();
        temp.transform.position = this.gameObject.transform.position;
        meshFilter = temp.GetComponent<MeshFilter>();
        temp.GetComponent<MeshRenderer>().material = material;
        meshFilter.mesh = MeshExtruder.Extrude(path, GetShapeArray(), u, fixedTotalV, vFactor);
        temp.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
        return temp;
    }

    public void PreviewMesh(GameObject game)
    {
        connection.UpdateCruves();
        previewMeshFilter = game.GetComponent<MeshFilter>();
        previewMeshFilter.mesh = MeshExtruder.Extrude(path, GetShapeArray(), u, fixedTotalV, vFactor);
    }

    public void PreviewMesh()
    {
        
        if (preview)
        {
            connection.UpdateCruves();
            previewMeshFilter = this.GetComponent<MeshFilter>();

            if (Application.isEditor)
            {
                DestroyImmediate(tempMesh);
            }
            else if (Application.isPlaying)
            {
                Destroy(tempMesh);
            }
        
            tempMesh= MeshExtruder.Extrude(path, GetShapeArray(), u, fixedTotalV, vFactor);
            previewMeshFilter.mesh = tempMesh;
            //Debug.Log("Preview On");
        }
        else
        {
            previewMeshFilter = this.GetComponent<MeshFilter>();
            if (previewMeshFilter.sharedMesh)
            {
                previewMeshFilter.sharedMesh = null;
            }
          //  Debug.Log("Preview Close");
        }

    }

}
