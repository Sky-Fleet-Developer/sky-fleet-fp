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

    public static Vector2 XY(this Vector3 value)
    {
        return new Vector2(value.x, value.y);
    }

    public static Vector2 XZ(this Vector3 value)
    {
        return new Vector2(value.x, value.z);
    }

    public static Vector2 YZ(this Vector3 value)
    {
        return new Vector2(value.y, value.z);
    }
    
    public static Vector3 XY(this Vector2 value)
    {
        return new Vector3(value.x, value.y, 0f);
    }
    
    public static Vector3 XZ(this Vector2 value)
    {
        return new Vector3(value.x, 0f, value.y);
    }
    
    public static Vector3 YZ(this Vector2 value)
    {
        return new Vector3(0f, value.x, value.y);
    }
    
}
