using System;
using UnityEngine;



namespace Extra.Utility
{
    public static class QuaternionExtensions
    {
        public static Quaternion SlerpWithReferenceUp(Quaternion qA, Quaternion qB, float t, Vector3 up)
        {
            Vector3 vector = (qA * Vector3.forward).ProjectOntoPlane(up);
            Vector3 v = (qB * Vector3.forward).ProjectOntoPlane(up);
            if (vector.AlmostZero() || v.AlmostZero())
            {
                return Quaternion.Slerp(qA, qB, t);
            }

            Quaternion quaternion = Quaternion.LookRotation(vector, up);
            Quaternion lhs = Quaternion.Inverse(quaternion);
            Quaternion quaternion2 = lhs * qA;
            Quaternion quaternion3 = lhs * qB;
            Vector3 eulerAngles = quaternion2.eulerAngles;
            Vector3 eulerAngles2 = quaternion3.eulerAngles;
            return quaternion * Quaternion.Euler(Mathf.LerpAngle(eulerAngles.x, eulerAngles2.x, t), Mathf.LerpAngle(eulerAngles.y, eulerAngles2.y, t), Mathf.LerpAngle(eulerAngles.z, eulerAngles2.z, t));
        }

        public static Quaternion Normalized(this Quaternion q)
        {
            Vector4 normalized = new Vector4(q.x, q.y, q.z, q.w).normalized;
            return new Quaternion(normalized.x, normalized.y, normalized.z, normalized.w);
        }

        public static Vector2 GetCameraRotationToTarget(this Quaternion orient, Vector3 lookAtDir, Vector3 worldUp)
        {
            if (lookAtDir.AlmostZero())
            {
                return Vector2.zero;
            }

            Quaternion rotation = Quaternion.Inverse(orient);
            Vector3 vector = rotation * worldUp;
            lookAtDir = rotation * lookAtDir;
            float num = 0f;
            Vector3 vector2 = lookAtDir.ProjectOntoPlane(vector);
            if (!vector2.AlmostZero())
            {
                Vector3 vector3 = Vector3.forward.ProjectOntoPlane(vector);
                if (vector3.AlmostZero())
                {
                    vector3 = ((!(Vector3.Dot(vector3, vector) > 0f)) ? Vector3.up.ProjectOntoPlane(vector) : Vector3.down.ProjectOntoPlane(vector));
                }

                num = VectorExtensions.SignedAngle(vector3, vector2, vector);
            }

            Quaternion rotation2 = Quaternion.AngleAxis(num, vector);
            return new Vector2(VectorExtensions.SignedAngle(rotation2 * Vector3.forward, lookAtDir, rotation2 * Vector3.right), num);
        }

        public static Quaternion ApplyCameraRotation(this Quaternion orient, Vector2 rot, Vector3 worldUp)
        {
            Quaternion rhs = Quaternion.AngleAxis(rot.x, Vector3.right);
            return Quaternion.AngleAxis(rot.y, worldUp) * orient * rhs;
        }
    }
    public static class VectorExtensions 
    {
        public const float Epsilon = 0.0001f;

        public static bool IsNaN(this Vector2 v)
        {
            if (!float.IsNaN(v.x))
            {
                return float.IsNaN(v.y);
            }

            return true;
        }

        public static bool IsNaN(this Vector3 v)
        {
            if (!float.IsNaN(v.x) && !float.IsNaN(v.y))
            {
                return float.IsNaN(v.z);
            }

            return true;
        }

        public static float ClosestPointOnSegment(this Vector3 p, Vector3 s0, Vector3 s1)
        {
            Vector3 vector = s1 - s0;
            float num = Vector3.SqrMagnitude(vector);
            if (num < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(Vector3.Dot(p - s0, vector) / num);
        }

        public static float ClosestPointOnSegment(this Vector2 p, Vector2 s0, Vector2 s1)
        {
            Vector2 vector = s1 - s0;
            float num = Vector2.SqrMagnitude(vector);
            if (num < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(Vector2.Dot(p - s0, vector) / num);
        }

        public static Vector3 ProjectOntoPlane(this Vector3 vector, Vector3 planeNormal)
        {
            return vector - Vector3.Dot(vector, planeNormal) * planeNormal;
        }

        public static Vector2 SquareNormalize(this Vector2 v)
        {
            float num = Mathf.Max(Mathf.Abs(v.x), Mathf.Abs(v.y));
            if (!(num < 0.0001f))
            {
                return v / num;
            }

            return Vector2.zero;
        }

        public static int FindIntersection(in Vector2 p1, in Vector2 p2, in Vector2 q1, in Vector2 q2, out Vector2 intersection)
        {
            Vector2 vector = p2 - p1;
            Vector2 vector2 = q2 - q1;
            Vector2 vector3 = q1 - p1;
            float num = vector.Cross(vector2);
            if (Mathf.Abs(num) < 1E-05f)
            {
                intersection = Vector2.positiveInfinity;
                if (Mathf.Abs(vector3.Cross(vector)) < 1E-05f)
                {
                    float num2 = Vector2.Dot(vector2, vector);
                    if (num2 > 0f && (p1 - q2).sqrMagnitude < 0.001f)
                    {
                        intersection = q2;
                        return 4;
                    }

                    if (num2 < 0f && (p2 - q2).sqrMagnitude < 0.001f)
                    {
                        intersection = p2;
                        return 4;
                    }

                    float num3 = Vector2.Dot(vector3, vector);
                    if (0f <= num3 && num3 <= Vector2.Dot(vector, vector))
                    {
                        if (num3 < 0.0001f)
                        {
                            if (num2 <= 0f && (p1 - q1).sqrMagnitude < 0.001f)
                            {
                                intersection = p1;
                            }
                        }
                        else if (num2 > 0f && (p2 - q1).sqrMagnitude < 0.001f)
                        {
                            intersection = p2;
                        }

                        return 4;
                    }

                    num3 = Vector2.Dot(p1 - q1, vector2);
                    if (0f <= num3 && num3 <= Vector2.Dot(vector2, vector2))
                    {
                        return 4;
                    }

                    return 3;
                }

                return 0;
            }

            float num4 = vector3.Cross(vector2) / num;
            intersection = p1 + num4 * vector;
            float num5 = vector3.Cross(vector) / num;
            if (0f <= num4 && num4 <= 1f && 0f <= num5 && num5 <= 1f)
            {
                return 2;
            }

            return 1;
        }

        private static float Cross(this Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static Vector2 Abs(this Vector2 v)
        {
            return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static bool IsUniform(this Vector2 v)
        {
            return Math.Abs(v.x - v.y) < 0.0001f;
        }

        public static bool IsUniform(this Vector3 v)
        {
            if (Math.Abs(v.x - v.y) < 0.0001f)
            {
                return Math.Abs(v.x - v.z) < 0.0001f;
            }

            return false;
        }

        public static bool AlmostZero(this Vector3 v)
        {
            return v.sqrMagnitude < 9.999999E-09f;
        }

        public static float Angle(Vector3 v1, Vector3 v2)
        {
            v1.Normalize();
            v2.Normalize();
            return Mathf.Atan2((v1 - v2).magnitude, (v1 + v2).magnitude) * 57.29578f * 2f;
        }

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 up)
        {
            float num = Angle(v1, v2);
            if (Mathf.Sign(Vector3.Dot(up, Vector3.Cross(v1, v2))) < 0f)
            {
                return 0f - num;
            }

            return num;
        }

        public static Quaternion SafeFromToRotation(Vector3 v1, Vector3 v2, Vector3 up)
        {
            Vector3 vector = Vector3.Cross(v1, v2);
            if (vector.AlmostZero())
            {
                vector = up;
            }

            return Quaternion.AngleAxis(Angle(v1, v2), vector);
        }

        public static Vector3 SlerpWithReferenceUp(Vector3 vA, Vector3 vB, float t, Vector3 up)
        {
            float magnitude = vA.magnitude;
            float magnitude2 = vB.magnitude;
            if (magnitude < 0.0001f || magnitude2 < 0.0001f)
            {
                return Vector3.Lerp(vA, vB, t);
            }

            Vector3 forward = vA / magnitude;
            Vector3 forward2 = vB / magnitude2;
            Quaternion qA = Quaternion.LookRotation(forward, up);
            Quaternion qB = Quaternion.LookRotation(forward2, up);
            return QuaternionExtensions.SlerpWithReferenceUp(qA, qB, t, up) * Vector3.forward * Mathf.Lerp(magnitude, magnitude2, t);
        }

    }
}

