using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SphereWorld
{
    [Serializable, InlineProperty(LabelWidth = 100)]
    public struct Polar
    {
        [Header("degrees")]
        public float latitude;
        public float longitude;
        [Header("kilometers")]
        public float height;

        public Polar(float latitude, float height, float longitude)
        {
            this.latitude = latitude;
            this.height = height;
            this.longitude = longitude;
        }

        public static Polar FromUniSphere(Vector3 value, float zeroHeight)
        {
            Polar result = value.ToPolar();
            result.height = (result.height - 1) * zeroHeight;
            return result;
        }

        // Vector3(latitude, height, longitude)
        public static implicit operator Vector3 (Polar polar)
        {
            return new Vector3(polar.latitude, polar.height, polar.longitude);
        }
        public static implicit operator Polar (Vector3 vector)
        {
            return new Polar(vector.x, vector.y, vector.z);
        }

        public static Polar operator +(Polar lhs, Polar rhs)
        {
            return new Polar(lhs.latitude + rhs.latitude, lhs.height + rhs.height, lhs.longitude + rhs.longitude);
        }
        
        public static Polar operator -(Polar lhs, Polar rhs)
        {
            return new Polar(lhs.latitude - rhs.latitude, lhs.height - rhs.height, lhs.longitude - rhs.longitude);
        }
        
        public static Polar operator -(Polar lhs)
        {
            return new Polar(-lhs.latitude, -lhs.height, -lhs.longitude);
        }
    }

    public static class CoordinatesExtension
    {
        public static Polar ToPolar(this Vector3 value)
        {
            Vector3 result = Vector3.zero;
            result.y = value.magnitude;
            result.x = Mathf.Acos(value.y / result.y);
            result.z = Mathf.Atan2(value.z, value.x);
            return result;
        }
        public static Vector3 ToGlobalWithHeight(this Polar value, float actualHeight)
        {
            return new Vector3(
                actualHeight * Mathf.Sin(value.latitude * Mathf.Deg2Rad) * Mathf.Cos(value.longitude * Mathf.Deg2Rad),
                actualHeight * Mathf.Cos(value.latitude * Mathf.Deg2Rad),
                actualHeight * Mathf.Sin(value.latitude * Mathf.Deg2Rad) * Mathf.Sin(value.longitude * Mathf.Deg2Rad));
        }
        public static Vector3 ToGlobal(this Polar value, float zeroHeightKm)
        {
            float actualHeight = zeroHeightKm + value.height;
            return ToGlobalWithHeight(value, actualHeight);
        }

        public static Polar ClampCircle(this Polar value)
        {
            return new Polar(value.latitude % 360, value.height, value.longitude % 360);
        }
    }
}