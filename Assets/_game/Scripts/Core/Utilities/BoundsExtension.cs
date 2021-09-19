using UnityEngine;

namespace Core.Utilities
{
    public static class BoundsExtension
    {
        public static Rect GetRectangle(this Bounds bounds, Camera cam)
        {
            Rect rect = new Rect();
            rect.center = cam.WorldToScreenPoint(bounds.center);
            Vector2 p = cam.WorldToScreenPoint(bounds.min + Vector3.right * bounds.size.x);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.min + Vector3.up * bounds.size.y);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.min + Vector3.forward * bounds.size.z);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.max);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.min + Vector3.up * bounds.size.y + Vector3.forward * bounds.size.z);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.min + Vector3.forward * bounds.size.z + Vector3.right * bounds.size.x);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);
            p = cam.WorldToScreenPoint(bounds.min + Vector3.up * bounds.size.y + Vector3.right * bounds.size.x);
            rect.min = Vector2.Min(rect.min, p);
            rect.max = Vector2.Max(rect.max, p);

            return rect;
        }
    }
}
