using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Core.TerrainGenerator.Utility;

namespace Core.TerrainGenerator.Editor
{
    public class GeneratorTreeLayer : ScriptableWizard
    {
        public string pathToPng;

        public string pathToSave;

        [Space]
        public int sizeRandomMap;

        [Range(0, 1)]
        public float minValueFilter;
        [Range(0.0f, 0.5f)]
        public float minDist;

        private Texture2D generateRandomPos;
        private Texture2D loadMapTrees;

        private TreesLayer treesLayer;

        public GeneratorTreeLayer()
        {
            for (int i = 0; i < ParamsBakingModels.Params.Length; i++)
            {
                ParamsBakingModels.Params[i].SetValue(this, ParamsBakingModels.ValueParams[i]);
            }
            this.hasUnsavedChanges = true;
        }

        [MenuItem("Factory/Tree layer generator")]
        public static void BakingModel()
        {
            DisplayWizard<GeneratorTreeLayer>("Tree layer generator", "Generate");
        }

        protected override bool DrawWizardGUI()
        {
            return base.DrawWizardGUI();
        }


        public override void SaveChanges()
        {
            Type type = this.GetType();
            FieldInfo[] params_type = type.GetFields();
            ParamsBakingModels.Params = params_type;
            ParamsBakingModels.ValueParams = new object[params_type.Length];
            for (int i = 0; i < params_type.Length; i++)
            {
                ParamsBakingModels.ValueParams[i] = params_type[i].GetValue(this);
            }
            base.SaveChanges();
        }

        private void OnWizardUpdate()
        {

        }

        void OnWizardCreate()
        {
            treesLayer = new TreesLayer(0, 0);
            CreateTexutreRandom();
            Texture2D[] textures = LoadTreesMaps();
            for (int i = 0; i < generateRandomPos.width; i++)
            {
                for (int i2 = 0; i2 < generateRandomPos.height; i2++)
                {
                    Color currectPixelR = generateRandomPos.GetPixel(i, i2);
                    for (int i3 = 0; i3 < textures.Length; i3++)
                    {
                        int sizeRect = textures[i3].width / sizeRandomMap;
                        Color[] colors = textures[i3].GetPixels(sizeRect * i, sizeRect * i2, sizeRect, sizeRect);
                        bool lockR = false;
                        bool lockG = false;
                        bool lockB = false;
                        for (int i4 = 0; i4 < colors.Length; i4++)
                        {

                            if (!lockR && colors[i4].a * colors[i4].r * currectPixelR.r > minValueFilter)
                            {
                                lockR = true;
                                Vector2 pos = new Vector2((float)i / sizeRandomMap, (float)i2 / sizeRandomMap) + (GetAdditionalPos() / sizeRandomMap);
                                treesLayer.Trees.Add(new TreePos(i4 * 3, 0, pos));
                            }
                            if (!lockG && colors[i4].a * colors[i4].g * currectPixelR.r > minValueFilter)
                            {
                                lockG = true;
                                Vector2 pos = new Vector2((float)i / sizeRandomMap, (float)i2 / sizeRandomMap) + (GetAdditionalPos() / sizeRandomMap);
                                treesLayer.Trees.Add(new TreePos(i4 * 3 + 1, 0, pos));
                            }
                            if (!lockB && colors[i4].a * colors[i4].b * currectPixelR.r > minValueFilter)
                            {
                                lockB = true;
                                Vector2 pos = new Vector2((float)i / sizeRandomMap, (float)i2 / sizeRandomMap) + (GetAdditionalPos() / sizeRandomMap);
                                treesLayer.Trees.Add(new TreePos(i4 * 3 + 2, 0, pos));
                            }
                        }
                    }
                }
            }
            Debug.Log(treesLayer.Trees.Count);
            TreesLayerFiles.SaveTreeLayer(pathToSave, treesLayer);
            SaveChanges();
        }

        private Vector2 GetAdditionalPos()
        {
            return new Vector2(UnityEngine.Random.Range(0.0f + minDist, 1.0f - minDist), UnityEngine.Random.Range(0.0f + minDist, 1.0f - minDist));
        }

        private void CreateTexutreRandom()
        {
            generateRandomPos = new Texture2D(sizeRandomMap, sizeRandomMap);
            for (int i = 0; i < sizeRandomMap; i++)
            {
                for (int i2 = 0; i2 < sizeRandomMap; i2++)
                {
                    generateRandomPos.SetPixel(i, i2, new Color(UnityEngine.Random.Range(0.0f, 1.0f), 0, 0));
                }
            }
        }

        private Texture2D[] LoadTreesMaps()
        {
            Texture2D texutre = new Texture2D(2, 2);
            byte[] buffer = File.ReadAllBytes(pathToPng);
            texutre.LoadImage(buffer);
            return new Texture2D[1] { texutre };
        }

        static class ParamsBakingModels
        {
            public static FieldInfo[] Params = new FieldInfo[0];
            public static object[] ValueParams = new object[0];
        }


    }
}