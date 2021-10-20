using UnityEngine;

public static class Vector3Extensions
{

    public static Vector3 ClampDistance(this Vector3 value, float Min, float Max)
    {
        if (value == Vector3.zero) return value;
        float m = value.magnitude;
        return Mathf.Clamp(m, Min, Max) * (value / m);
    }

    public static float AxeMagnitude(this Vector3 value, Vector3 point)
    {
        Vector3 delta = value - point;
        return Mathf.Min(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
    }
}
