using UnityEngine;

namespace Core.Trees
{
    /// <summary>
    /// Layout of the views of a tree impostor: a hemisphere of directions (the tree is seen from the side and from
    /// above, never from below) mapped to a square by the hemi-octahedral mapping, cut into a grid of frames. The
    /// baker renders a frame from the direction of its center; the impostor shader picks the frames around the
    /// direction to the camera. The shader has the same functions (Visual/Trees/Impostor.hlsl): change both together.
    /// </summary>
    public static class ImpostorLayout
    {
        /// <summary>Unit direction from the tree towards the viewer (up is +y) for a point of the square, uv 0..1.</summary>
        public static Vector3 Decode(Vector2 uv)
        {
            float ex = uv.x * 2 - 1;
            float ey = uv.y * 2 - 1;
            float x = (ex + ey) * 0.5f;
            float z = (ex - ey) * 0.5f;
            float y = 1 - Mathf.Abs(x) - Mathf.Abs(z);
            return new Vector3(x, y, z).normalized;
        }

        /// <summary>The point of the square (uv 0..1) of a direction; a direction below the horizon is taken at it.</summary>
        public static Vector2 Encode(Vector3 direction)
        {
            direction.y = Mathf.Max(direction.y, 0);
            float sum = Mathf.Abs(direction.x) + direction.y + Mathf.Abs(direction.z);
            float x = direction.x / sum;
            float z = direction.z / sum;
            return new Vector2(x + z, x - z) * 0.5f + new Vector2(0.5f, 0.5f);
        }

        /// <summary>Direction towards the viewer of the frame (column, row) of a grid of framesPerSide.</summary>
        public static Vector3 FrameDirection(int column, int row, int framesPerSide) =>
            Decode(new Vector2((column + 0.5f) / framesPerSide, (row + 0.5f) / framesPerSide));

        /// <summary>
        /// The axes of the picture of a frame seen from a direction: the camera looks along -direction, up is as close
        /// to the world up as it can be (straight above, the picture is turned to the world's z).
        /// </summary>
        public static void FrameAxes(Vector3 direction, out Vector3 right, out Vector3 up)
        {
            Vector3 forward = -direction;
            Vector3 hint = Mathf.Abs(forward.y) > 0.999f ? Vector3.forward : Vector3.up;
            right = Vector3.Cross(hint, forward).normalized;
            up = Vector3.Cross(forward, right);
        }
    }
}
