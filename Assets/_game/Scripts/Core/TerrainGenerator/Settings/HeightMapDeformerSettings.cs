using UnityEngine;

namespace Core.TerrainGenerator.Settings
{
    [System.Serializable]
    public class HeightMapDeformerSettings : IDeformerLayerSetting
    {
        public float[] Heights;
        public Vector2Int Resolution;

        public Deformer Core { get; set; }
        public void Init(Deformer core)
        {
            Core = core;
        }
        
        public void ReadFromTerrain(Terrain[] terrain)
        {
            TerrainCollider[] colliders = new TerrainCollider[terrain.Length];
            for(int i = 0; i < terrain.Length; i++)
            {
                colliders[i] = terrain[i].GetComponent<TerrainCollider>();
            }

            
            Heights = new float[Resolution.x * Resolution.y];
            for(int i = 0; i < Resolution.x; i++)
            {
                for (int i2 = 0; i2 < Resolution.y; i2++)
                {
                    Vector3 pos = Core.transform.rotation * new Vector3(i - Resolution.x, 6000, i2 - Resolution.y);
                    pos += Core.transform.position;
                    Ray ray = new Ray(pos, -Vector3.up);
                    RaycastHit hit;
                    for(int i3 = 0; i3 < colliders.Length; i3++)
                    {
                        if(colliders[i].Raycast(ray, out hit, float.MaxValue))
                        {
                            Heights[i * Resolution.x + i2] = hit.point.y - Core.transform.position.y;
                            break;
                        }
                    }
                }
            }
        }

        public void WriteToTerrain(Terrain[] terrain)
        {

        }
    }
}