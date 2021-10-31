using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.TerrainGenerator;
using Core.TerrainGenerator.Utility;


public class TestLoadTrees : MonoBehaviour
{
    [SerializeField] private string path;
    [SerializeField] private GameObject prefab;
    [Space]
    [SerializeField] private float area;

    /*[Button]
    private void Generate()
    {
        TreesLayer layer = new TreesLayer(0, 0, path);
        foreach(TreePos pos in layer.Trees)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.transform.position = new Vector3(pos.Pos.x * area, 0, pos.Pos.y * area);
        }
    }*/
}
