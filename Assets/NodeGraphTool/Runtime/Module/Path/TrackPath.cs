using UnityEngine;
using System;
using Extra.Utility;

[AddComponentMenu("Tools/TrackPath")]
[DisallowMultipleComponent]
public class TrackPath : PathBase
{
    [Serializable]
    public struct Waypoint
    {
        /// <summary>Position in path-local space</summary>
        public Vector3 position;

        /// <summary>Offset from the position, which defines the tangent of the curve at the waypoint.  
        /// The length of the tangent encodes the strength of the bezier handle.  
        /// The same handle is used symmetrically on both sides of the waypoint, to ensure smoothness.</summary>    
        public Vector3 tangent;
        /// <summary>Defines the roll of the path at this waypoint.  
        /// The other orientation axes are inferred from the tangent and world up.</summary>
        public float roll;
    }
    public bool m_Looped;
    public Waypoint[] m_Waypoints = new Waypoint[0];
    public override float MinPos { get { return 0; } }
    public override float MaxPos
    {
        get
        {
            int count = m_Waypoints.Length - 1;
            if (count < 1)
                return 0;
            return m_Looped ? count + 1 : count;
        }
    }
    public override bool Looped { get { return m_Looped; } }

    private void Reset()
    {
        m_Looped = false;
        m_Waypoints = new Waypoint[2]
        {
                new Waypoint { position = new Vector3(0, 0, -5), tangent = new Vector3(1, 0, 0) },
                new Waypoint { position = new Vector3(0, 0, 5), tangent = new Vector3(1, 0, 0) }
        };
        m_Appearance = new Appearance();
        InvalidateDistanceCache();
    }
    public override int DistanceCacheSampleStepsPerSegment { get { return m_Resolution; } }
    protected float GetBoundingIndices(float pos, out int indexA, out int indexB)
    {
        pos = StandardizePos(pos);
        int rounded = Mathf.RoundToInt(pos);
        if (Mathf.Abs(pos - rounded) < VectorExtensions.Epsilon)
            indexA = indexB = (rounded == m_Waypoints.Length) ? 0 : rounded;
        else
        {
            indexA = Mathf.FloorToInt(pos);
            if (indexA >= m_Waypoints.Length)
            {
                pos -= MaxPos;
                indexA = 0;
            }
            indexB = Mathf.CeilToInt(pos);
            if (indexB >= m_Waypoints.Length)
                indexB = 0;
        }
        return pos;
    }


    /// <summary>Get a worldspace position of a point along the path</summary>
    /// <param name="pos">Postion along the path.  Need not be normalized.</param>
    /// <returns>World-space position of the point along at path at pos</returns>
    public override Vector3 EvaluatePosition(float pos)
    {
        Vector3 result = new Vector3();
        if (m_Waypoints.Length == 0)
            result = transform.position;
        else
        {
            int indexA, indexB;
            pos = GetBoundingIndices(pos, out indexA, out indexB);
            if (indexA == indexB)
                result = m_Waypoints[indexA].position;
            else
            {
                // interpolate
                Waypoint wpA = m_Waypoints[indexA];
                Waypoint wpB = m_Waypoints[indexB];
                result = BezierHelpers.Bezier3(pos - indexA,
                    m_Waypoints[indexA].position, wpA.position + wpA.tangent,
                    wpB.position - wpB.tangent, wpB.position);
            }
        }
        return transform.TransformPoint(result);
    }

    /// <summary>Get the tangent of the curve at a point along the path.</summary>
    /// <param name="pos">Postion along the path.  Need not be normalized.</param>
    /// <returns>World-space direction of the path tangent.
    /// Length of the vector represents the tangent strength</returns>
    public override Vector3 EvaluateTangent(float pos)
    {
        Vector3 result = new Vector3();
        if (m_Waypoints.Length == 0)
            result = transform.rotation * Vector3.forward;
        else
        {
            int indexA, indexB;
            pos = GetBoundingIndices(pos, out indexA, out indexB);
            if (indexA == indexB)
                result = m_Waypoints[indexA].tangent;
            else
            {
                Waypoint wpA = m_Waypoints[indexA];
                Waypoint wpB = m_Waypoints[indexB];
                result = BezierHelpers.BezierTangent3(pos - indexA,
                    m_Waypoints[indexA].position, wpA.position + wpA.tangent,
                    wpB.position - wpB.tangent, wpB.position);
            }
        }
        return transform.TransformDirection(result);
    }

    /// <summary>Get the orientation the curve at a point along the path.</summary>
    /// <param name="pos">Postion along the path.  Need not be normalized.</param>
    /// <returns>World-space orientation of the path, as defined by tangent, up, and roll.</returns>
    public override Quaternion EvaluateOrientation(float pos)
    {
        Quaternion result = transform.rotation;
        if (m_Waypoints.Length > 0)
        {
            float roll = 0;
            int indexA, indexB;
            pos = GetBoundingIndices(pos, out indexA, out indexB);
            if (indexA == indexB)
                roll = m_Waypoints[indexA].roll;
            else
            {
                float rollA = m_Waypoints[indexA].roll;
                float rollB = m_Waypoints[indexB].roll;
                if (indexB == 0)
                {
                    // Special handling at the wraparound - cancel the spins
                    rollA = rollA % 360;
                    rollB = rollB % 360;
                }
                roll = Mathf.Lerp(rollA, rollB, pos - indexA);
            }

            Vector3 fwd = EvaluateTangent(pos);
            if (!fwd.AlmostZero())
            {
                Vector3 up = transform.rotation * Vector3.up;
                Quaternion q = Quaternion.LookRotation(fwd, up);
                result = q * Quaternion.AngleAxis(roll, Vector3.forward);
            }
        }
        return result;
    }

    private void OnValidate() { InvalidateDistanceCache(); }
}
