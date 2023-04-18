using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;

public class TerrainColorAnalysis : MonoBehaviour
{
    [SerializeField] private Terrain terrain;
    [SerializeField] private Vector2Int beginCoordinate;
    [SerializeField] private Vector2Int direction;
    [SerializeField] private int length;
    [SerializeField] private int resultResolution = 30;
    [SerializeField] private Color[] colors = {Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow};
    [SerializeField] private Color sumColor = Color.white;
    [SerializeField, InlineProperty] private List<Texture2D> result;
    [SerializeField, FolderPath(AbsolutePath = true)] private string outputFolder;

    [Button]
    private void MakeAnalysis()
    {
        TerrainData data = terrain.terrainData;
        float[,,] alphamap = data.GetAlphamaps(0, 0, data.alphamapResolution, data.alphamapResolution);
        Texture2D texture = new Texture2D(length, resultResolution, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[length * resultResolution];

        for (int i = 0; i < length; i++)
        {
            int sum = 0;
            for (int channel = 0; channel < data.alphamapLayers; channel++)
            {
                int alpha = 2 + (int) (alphamap[beginCoordinate.x + direction.x * i, beginCoordinate.y + direction.y * i,
                    channel] * (resultResolution * 0.5f));
                sum += alpha;
                pixels[alpha * length + i] = colors[channel];
            }

            if (sum < resultResolution)
            {
                pixels[sum * length + i] = sumColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        result.Add(texture);

        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(outputFolder + $"/result_{result.Count}.png", bytes);
    }
}