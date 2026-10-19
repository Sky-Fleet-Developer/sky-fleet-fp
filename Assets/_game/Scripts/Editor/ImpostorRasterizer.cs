using Core.Trees;
using UnityEngine;

namespace GameEditor.Trees
{
    /// <summary>A mesh ready to be baked: world positions, normals and the color of every vertex (shading is done by the baker).</summary>
    public class ImpostorMesh
    {
        public Vector3[] positions;
        public Vector3[] normals;
        public Color[] colors;
        public int[] triangles;
    }

    /// <summary>
    /// Renders the frames of an impostor atlas on the CPU: an orthographic view of the mesh from a direction of the
    /// hemisphere, nearest surface wins, both faces count. Albedo and alpha (coverage) go into one atlas, the normal
    /// (object space) and the depth into another. Frames are rendered at a multiple of their size and averaged.
    /// </summary>
    public static class ImpostorRasterizer
    {
        /// <summary>The center and radius of the smallest sphere around the center of the bounds that holds the mesh.</summary>
        public static void Bound(ImpostorMesh mesh, out Vector3 center, out float radius)
        {
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var position in mesh.positions)
            {
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }

            center = (min + max) * 0.5f;
            var farthest = 0f;
            foreach (var position in mesh.positions) farthest = Mathf.Max(farthest, (position - center).sqrMagnitude);
            radius = Mathf.Sqrt(farthest) * 1.01f;
        }

        /// <summary>
        /// Both atlases are framesPerSide * frameSize pixels square, row 0 at the bottom; frame (column, row) lies at
        /// column * frameSize, row * frameSize.
        /// </summary>
        public static void Bake(ImpostorMesh mesh, Vector3 center, float radius, int framesPerSide, int frameSize, int supersample,
            int padding, out Color32[] albedo, out Color32[] normalDepth)
        {
            var atlasSize = framesPerSide * frameSize;
            albedo = new Color32[atlasSize * atlasSize];
            normalDepth = new Color32[atlasSize * atlasSize];
            // Depth is stored over the whole depth of a frame, 2 * radius
            var buffer = new Frame(frameSize * supersample) { depthScale = radius * 2 };

            for (var row = 0; row < framesPerSide; row++)
            {
                for (var column = 0; column < framesPerSide; column++)
                {
                    var direction = ImpostorLayout.FrameDirection(column, row, framesPerSide);
                    Render(mesh, center, radius, direction, buffer);
                    Resolve(buffer, supersample, frameSize, column * frameSize, row * frameSize, atlasSize, albedo, normalDepth);
                    Dilate(albedo, normalDepth, atlasSize, column * frameSize, row * frameSize, frameSize, padding);
                }
            }
        }

        private class Frame
        {
            public readonly int size;
            public float depthScale;
            public readonly float[] depth;
            public readonly Color[] color;
            public readonly Vector3[] normal;

            public Frame(int size)
            {
                this.size = size;
                depth = new float[size * size];
                color = new Color[size * size];
                normal = new Vector3[size * size];
            }
        }

        private static void Render(ImpostorMesh mesh, Vector3 center, float radius, Vector3 direction, Frame frame)
        {
            ImpostorLayout.FrameAxes(direction, out var right, out var up);
            var forward = -direction;
            var size = frame.size;
            for (var i = 0; i < frame.depth.Length; i++) frame.depth[i] = float.MaxValue;

            var count = mesh.positions.Length;
            var screen = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var local = mesh.positions[i] - center;
                // x and y in pixels, z the distance behind the near side of the frame in meters
                screen[i] = new Vector3(
                    (Vector3.Dot(local, right) / radius * 0.5f + 0.5f) * size,
                    (Vector3.Dot(local, up) / radius * 0.5f + 0.5f) * size,
                    Vector3.Dot(local, forward) + radius);
            }

            for (var t = 0; t + 2 < mesh.triangles.Length; t += 3)
            {
                var a = mesh.triangles[t];
                var b = mesh.triangles[t + 1];
                var c = mesh.triangles[t + 2];
                var pa = screen[a];
                var pb = screen[b];
                var pc = screen[c];
                var area = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
                if (Mathf.Abs(area) < 1e-9f) continue;

                var minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.x, pb.x, pc.x)));
                var maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(pa.x, pb.x, pc.x)));
                var minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(pa.y, pb.y, pc.y)));
                var maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(pa.y, pb.y, pc.y)));
                for (var y = minY; y <= maxY; y++)
                {
                    for (var x = minX; x <= maxX; x++)
                    {
                        var px = x + 0.5f;
                        var py = y + 0.5f;
                        var w0 = ((pb.x - px) * (pc.y - py) - (pb.y - py) * (pc.x - px)) / area;
                        var w1 = ((pc.x - px) * (pa.y - py) - (pc.y - py) * (pa.x - px)) / area;
                        var w2 = 1 - w0 - w1;
                        if (w0 < 0 || w1 < 0 || w2 < 0) continue;

                        var depth = w0 * pa.z + w1 * pb.z + w2 * pc.z;
                        var index = y * size + x;
                        if (depth >= frame.depth[index]) continue;

                        var normal = (mesh.normals[a] * w0 + mesh.normals[b] * w1 + mesh.normals[c] * w2).normalized;
                        // Both faces count: a surface turned away from the viewer is seen from behind
                        if (Vector3.Dot(normal, forward) > 0) normal = -normal;
                        frame.depth[index] = depth;
                        frame.color[index] = mesh.colors[a] * w0 + mesh.colors[b] * w1 + mesh.colors[c] * w2;
                        frame.normal[index] = normal;
                    }
                }
            }
        }

        // Averages blocks of samples into the pixels of the frame: alpha is the share covered
        private static void Resolve(Frame frame, int supersample, int frameSize, int offsetX, int offsetY, int atlasSize,
            Color32[] albedo, Color32[] normalDepth)
        {
            var samples = supersample * supersample;
            for (var y = 0; y < frameSize; y++)
            {
                for (var x = 0; x < frameSize; x++)
                {
                    var color = Vector3.zero;
                    var normal = Vector3.zero;
                    var depthSum = 0f;
                    var covered = 0;
                    for (var sy = 0; sy < supersample; sy++)
                    {
                        for (var sx = 0; sx < supersample; sx++)
                        {
                            var index = (y * supersample + sy) * frame.size + x * supersample + sx;
                            if (frame.depth[index] == float.MaxValue) continue;
                            covered++;
                            var c = frame.color[index];
                            color += new Vector3(c.r, c.g, c.b);
                            normal += frame.normal[index];
                            depthSum += frame.depth[index];
                        }
                    }

                    var target = (offsetY + y) * atlasSize + offsetX + x;
                    if (covered == 0)
                    {
                        albedo[target] = new Color32(0, 0, 0, 0);
                        normalDepth[target] = new Color32(128, 128, 255, 0);
                        continue;
                    }

                    color /= covered;
                    normal = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
                    albedo[target] = new Color32(Byte(color.x), Byte(color.y), Byte(color.z), Byte(covered / (float) samples));
                    normalDepth[target] = new Color32(Byte(normal.x * 0.5f + 0.5f), Byte(normal.y * 0.5f + 0.5f),
                        Byte(normal.z * 0.5f + 0.5f), Byte(depthSum / covered / frame.depthScale));
                }
            }
        }

        // Colors of the pixels next to the tree are copied into the empty ones around it, so filtering and mip levels
        // don't pull in the black of the background. Alpha stays 0 there
        private static void Dilate(Color32[] albedo, Color32[] normalDepth, int atlasSize, int offsetX, int offsetY, int frameSize,
            int padding)
        {
            var filled = new bool[frameSize * frameSize];
            for (var y = 0; y < frameSize; y++)
            {
                for (var x = 0; x < frameSize; x++) filled[y * frameSize + x] = albedo[(offsetY + y) * atlasSize + offsetX + x].a > 0;
            }

            for (var pass = 0; pass < padding; pass++)
            {
                var added = new System.Collections.Generic.List<int>();
                for (var y = 0; y < frameSize; y++)
                {
                    for (var x = 0; x < frameSize; x++)
                    {
                        if (filled[y * frameSize + x]) continue;
                        var sourceX = -1;
                        var sourceY = -1;
                        for (var dy = -1; dy <= 1 && sourceX < 0; dy++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nx = x + dx;
                                var ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= frameSize || ny >= frameSize || !filled[ny * frameSize + nx]) continue;
                                sourceX = nx;
                                sourceY = ny;
                                break;
                            }
                        }

                        if (sourceX < 0) continue;
                        var from = (offsetY + sourceY) * atlasSize + offsetX + sourceX;
                        var to = (offsetY + y) * atlasSize + offsetX + x;
                        var source = albedo[from];
                        albedo[to] = new Color32(source.r, source.g, source.b, 0);
                        normalDepth[to] = normalDepth[from];
                        added.Add(y * frameSize + x);
                    }
                }

                foreach (var index in added) filled[index] = true;
            }
        }

        private static byte Byte(float value) => (byte) Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
    }
}
