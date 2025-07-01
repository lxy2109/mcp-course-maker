using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using InspectorExtra;
#endif


[ExecuteAlways]
public class TrackCart : MonoBehaviour
{

    public TrackPath path;


    public enum UpdateMethod
    {
        /// <summary>Updated in normal MonoBehaviour Update.</summary>
        Update,
        /// <summary>Updated in sync with the Physics module, in FixedUpdate</summary>
        FixedUpdate,
        /// <summary>Updated in normal MonoBehaviour LateUpdate</summary>
        LateUpdate
    };
#if UNITY_EDITOR
    [FieldName("更新方式")]
#endif
    public UpdateMethod updateMethod = UpdateMethod.Update;
#if UNITY_EDITOR
    [FieldName("尺度")]
#endif
    public PathBase.PositionUnits positionUnits = PathBase.PositionUnits.Distance;

    [HideInInspector]
    public float speed=0;
#if UNITY_EDITOR
    [FieldName("当前尺度")]
#endif
    public float currpentPosition;

    void FixedUpdate()
    {
        if (updateMethod == UpdateMethod.FixedUpdate)
            SetCartPosition(currpentPosition + speed * Time.deltaTime);
    }

    void Update()
    {
        float tempspeed = Application.isPlaying ? speed : 0;
        if (updateMethod == UpdateMethod.Update)
            SetCartPosition(currpentPosition + tempspeed * Time.deltaTime);
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            SetCartPosition(currpentPosition);
        else if (updateMethod == UpdateMethod.LateUpdate)
            SetCartPosition(currpentPosition + speed * Time.deltaTime);
    }

    void SetCartPosition(float distanceAlongPath)
    {
        if (path != null)
        {
            currpentPosition = path.StandardizeUnit(distanceAlongPath, positionUnits);
            transform.position = path.EvaluatePositionAtUnit(currpentPosition, positionUnits);
            transform.rotation = path.EvaluateOrientationAtUnit(currpentPosition, positionUnits);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1, 0.2f, 0.3f);
        Gizmos.DrawMesh(Resources.Load<Mesh>("Gizmos/carmesh"),0,this.transform.position,this.transform.rotation*Quaternion.Euler(0,90,0));
    }

}
