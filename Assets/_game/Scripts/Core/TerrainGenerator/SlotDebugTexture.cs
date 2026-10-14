using UnityEngine;

namespace Core.TerrainGenerator
{
    /// <summary>
    /// Builds the texture of the <see cref="TerrainViewMode.Uv"/> view: a grid of side x side cells, one per texture slot,
    /// cell (x, y) starting at uv (x, y) / side. Each cell has its own color, the slot coordinates written as "x y"
    /// (same as HeightmapDataDrawer shows them) and a marker at the chunk uv origin.
    /// </summary>
    public static class SlotDebugTexture
    {
        // Seven segment digits, bit order: top, top right, bottom right, bottom, bottom left, top left, middle
        private static readonly byte[] DigitSegments = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };
        private static readonly Color32 BorderColor = new (20, 20, 20, 255);
        private static readonly Color32 TextColor = new (255, 255, 255, 255);
        private static readonly Color32 TextShadowColor = new (0, 0, 0, 255);
        private static readonly Color32 OriginColor = new (255, 0, 0, 255);

        public static Texture2D Create(int side, int cellSize = 128)
        {
            int size = side * cellSize;
            var pixels = new Color32[size * size];
            for (int x = 0; x < side; x++)
            {
                for (int y = 0; y < side; y++)
                {
                    DrawCell(pixels, size, x, y, side, cellSize);
                }
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, false)
            {
                name = "Terrain slots debug",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static void DrawCell(Color32[] pixels, int size, int slotX, int slotY, int side, int cellSize)
        {
            int x0 = slotX * cellSize;
            int y0 = slotY * cellSize;
            float hue = (slotX * side + slotY) / (float)(side * side);
            float value = (slotX + slotY) % 2 == 0 ? 0.85f : 0.6f;
            Color32 background = Color.HSVToRGB(hue, 0.65f, value);
            FillRect(pixels, size, x0, y0, cellSize, cellSize, BorderColor);
            int border = Mathf.Max(1, cellSize / 40);
            FillRect(pixels, size, x0 + border, y0 + border, cellSize - border * 2, cellSize - border * 2, background);

            int marker = cellSize / 8;
            FillRect(pixels, size, x0 + border, y0 + border, marker, marker, OriginColor);

            string text = $"{slotX} {slotY}";
            float digitHeight = cellSize * 0.4f;
            float digitWidth = Mathf.Min(cellSize * 0.22f, cellSize * 0.8f / (text.Length * 1.3f));
            float spacing = digitWidth * 0.3f;
            float thickness = Mathf.Max(1f, digitWidth * 0.25f);
            float textWidth = text.Length * digitWidth + (text.Length - 1) * spacing;
            float textX = x0 + (cellSize - textWidth) * 0.5f;
            float textY = y0 + (cellSize - digitHeight) * 0.5f;
            int shadow = Mathf.Max(1, Mathf.RoundToInt(thickness * 0.3f));
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    continue;
                }

                int digit = text[i] - '0';
                float digitX = textX + i * (digitWidth + spacing);
                DrawDigit(pixels, size, digit, digitX + shadow, textY - shadow, digitWidth, digitHeight, thickness, TextShadowColor);
                DrawDigit(pixels, size, digit, digitX, textY, digitWidth, digitHeight, thickness, TextColor);
            }
        }

        // y grows upwards, as uv does
        private static void DrawDigit(Color32[] pixels, int size, int digit, float x, float y, float width, float height,
            float thickness, Color32 color)
        {
            byte segments = DigitSegments[digit];
            int ix = Mathf.RoundToInt(x);
            int iy = Mathf.RoundToInt(y);
            int w = Mathf.RoundToInt(width);
            int h = Mathf.RoundToInt(height);
            int t = Mathf.RoundToInt(thickness);
            int half = (h + t) / 2;
            if ((segments & 1 << 0) != 0) FillRect(pixels, size, ix, iy + h - t, w, t, color);
            if ((segments & 1 << 1) != 0) FillRect(pixels, size, ix + w - t, iy + h - half, t, half, color);
            if ((segments & 1 << 2) != 0) FillRect(pixels, size, ix + w - t, iy, t, half, color);
            if ((segments & 1 << 3) != 0) FillRect(pixels, size, ix, iy, w, t, color);
            if ((segments & 1 << 4) != 0) FillRect(pixels, size, ix, iy, t, half, color);
            if ((segments & 1 << 5) != 0) FillRect(pixels, size, ix, iy + h - half, t, half, color);
            if ((segments & 1 << 6) != 0) FillRect(pixels, size, ix, iy + (h - t) / 2, w, t, color);
        }

        private static void FillRect(Color32[] pixels, int size, int x, int y, int width, int height, Color32 color)
        {
            int xMax = Mathf.Min(size, x + width);
            int yMax = Mathf.Min(size, y + height);
            for (int py = Mathf.Max(0, y); py < yMax; py++)
            {
                for (int px = Mathf.Max(0, x); px < xMax; px++)
                {
                    pixels[py * size + px] = color;
                }
            }
        }
    }
}
