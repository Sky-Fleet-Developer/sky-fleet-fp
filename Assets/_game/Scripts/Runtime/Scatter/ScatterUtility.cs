using UnityEngine;

namespace Runtime.Scatter
{
    public static class ScatterUtility
    {
        /// <summary>A structured buffer of exactly this size, recreated only when it changes.</summary>
        public static void EnsureBuffer(ref GraphicsBuffer buffer, int count, int stride)
        {
            if (buffer != null && buffer.count == count && buffer.stride == stride)
            {
                return;
            }

            buffer?.Release();
            buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, stride);
        }

        public static void DestroyObject(Object target)
        {
            if (!target)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
