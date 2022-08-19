using System.Collections.Generic;
using UnityEngine;

namespace Core.TerrainGenerator.Utility
{
    public static class MathfUtilities
    {
        public static (Vector2 min, Vector2 max) GetAxisAlignedRect(Vector4 localRect, Quaternion rotation,
            Vector3 position)
        {
            Vector3 leftDown = rotation * new Vector3(localRect.x - localRect.z * 0.5f, 0, localRect.y - localRect.w * 0.5f) + position;
            Vector3 leftUp = rotation * new Vector3(localRect.x - localRect.z * 0.5f, 0, localRect.y + localRect.w * 0.5f) + position;
            Vector3 rightDown = rotation * new Vector3(localRect.x + localRect.z * 0.5f, 0, localRect.y - localRect.w * 0.5f) + position;
            Vector3 rightUp = rotation * new Vector3(localRect.x + localRect.z * 0.5f, 0, localRect.y + localRect.w * 0.5f) + position;
            Vector2 min = new Vector2(Mathf.Min(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Min(leftDown.z, leftUp.z, rightDown.z, rightUp.z));
            Vector2 max = new Vector2(Mathf.Max(leftDown.x, leftUp.x, rightDown.x, rightUp.x), Mathf.Max(leftDown.z, leftUp.z, rightDown.z, rightUp.z));
            return (min, max);
        }
        
        public static List<Vector2Int> GetAffectChunks(float chunkSize, Vector3 right, Vector4 rect, Vector3 forward, Vector3 position)
        {
            Vector3[] array =
            {
                right * (-rect.z * 0.5f) + forward * (rect.w * 0.5f) + position,
                right * (rect.z * 0.5f) + forward * (rect.w * 0.5f) + position,
                right * (rect.z * 0.5f) + forward * (-rect.w * 0.5f) + position,
                right * (-rect.z * 0.5f) + forward * (-rect.w * 0.5f) + position,
            };

            var result = new List<Vector2Int>();
            foreach (Vector3 point in array)
            {
                Vector2Int newItem =
                    new Vector2Int(Mathf.FloorToInt(point.x / chunkSize), Mathf.FloorToInt(point.z / chunkSize));
                if (result.Contains(newItem) == false) result.Add(newItem);
            }

            return result;
        }
        
        public static Terrain[] GetTerrainsContacts(Vector3 right, Vector4 rect, Vector3 forward,
            Vector3 position)
        {
            Vector3 up = Vector3.up * 6000;
            Ray[] rays = new Ray[4];
            rays[0] = new Ray(up + right * -rect.z * 0.5f + forward * rect.w * 0.5f + position, Vector3.down);
            rays[1] = new Ray(up + right * rect.z * 0.5f + forward * rect.w * 0.5f + position, Vector3.down);
            rays[2] = new Ray(up + right * rect.z * 0.5f + forward * -rect.w * 0.5f + position, Vector3.down);
            rays[3] = new Ray(up + right * -rect.z * 0.5f + forward * -rect.w * 0.5f + position, Vector3.down);
            List<Terrain> terrains = new List<Terrain>();
            for (int i = 0; i < rays.Length; i++)
            {
                RaycastHit[] hits = Physics.RaycastAll(rays[i], Mathf.Infinity);
                foreach (RaycastHit hit in hits)
                {
                    Terrain terr = hit.collider.gameObject.GetComponent<Terrain>();
                    if (terr != null)
                    {
                        if (terrains.Find(
                            new System.Predicate<Terrain>(x => { return x.terrainData == terr.terrainData; })) == null)
                        {
                            terrains.Add(terr);
                        }
                    }
                }
            }

            return terrains.ToArray();
        }
        public static Rect GetAffectRectangle(Terrain terrain, Rect rect)
        {
            Vector3 pos = terrain.transform.position;
            rect.position -= pos.XZ();

            float terrainSizeInv = 1f / terrain.terrainData.size.x;

            rect.position *= terrainSizeInv;
            rect.size *= terrainSizeInv;

            return rect;
        }
        public static Rect GetAffectRectangle(TerrainData data, Vector3 terrainPosition, Rect rect)
        {
            rect.position -= terrainPosition.XZ();

            float terrainSizeInv = 1f / data.size.x;

            rect.position *= terrainSizeInv;
            rect.size *= terrainSizeInv;

            return rect;
        }
    }
}