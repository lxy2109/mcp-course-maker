using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AssemblySystem;
using Unity.Mathematics;

public class ConnectionRenderer : MonoBehaviour
{
    public Connection connection;

    public float anchorPointOffset;
    public float controlPointDist;

    public GameObject plugA;
    public GameObject plugB;
    public Vector3 plugPositionOffset;
    public Vector3 plugRotationOffset;

    public BezierPipeRenderer bezierPipeRenderer;
    // Start is called before the first frame update
    void Start()
    {
        if(connection == null)
        {
            connection = GetComponent<Connection>();
        }
    }
    // Update is called once per frame
    void Update()
    {
        UpdateBezierCurves();
        UpdatePlugTransform();
    }

    public void UpdateCruves()
    {
        UpdateBezierCurves();
        UpdatePlugTransform();
    }


    void UpdateBezierCurves()
    {
        SetBezierCurvePoints();
    }
    void UpdatePlugTransform()
    {
        plugA.transform.position = connection[0].position;
        plugA.transform.rotation = connection[0].rotation;
        plugB.transform.position = connection[1].position;
        plugB.transform.rotation = connection[1].rotation;

        plugA.transform.position += plugPositionOffset;
        plugA.transform.Rotate(plugRotationOffset);
        plugB.transform.position += plugPositionOffset;
        plugB.transform.Rotate(plugRotationOffset);
    }
    void SetBezierCurvePoints()
    {
        Vector3 p0 = connection[0].position + connection[0].up * anchorPointOffset;
        Vector3 p1 = p0 + connection[0].up * controlPointDist;
        Vector3 p3 = connection[1].position + connection[1].up * anchorPointOffset;
        Vector3 p2 = p3 + connection[1].up * controlPointDist;
        bezierPipeRenderer.bezierCurve.SetPoints(p0, p1, p2, p3);
    }

    public void SetFollow(bool b, Vector3 pos)
    {
        
        if (!b)
        {
            plugB.transform.position = pos;
            plugA.GetComponent<MeshRenderer>().enabled = false;
            plugB.GetComponent<MeshRenderer>().enabled = false;
            bezierPipeRenderer.preview = false;
        }
        else
        {
            plugA.transform.position = pos;
            plugB.transform.position = pos;
            plugA.GetComponent<MeshRenderer>().enabled = true;
            plugB.GetComponent<MeshRenderer>().enabled = true;
            bezierPipeRenderer.preview = true;
        }

    }
}
