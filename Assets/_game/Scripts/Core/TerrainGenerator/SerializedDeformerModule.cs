using System;
using Core.TerrainGenerator.Settings;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.TerrainGenerator
{
    [Serializable]
    public class SerializedDeformerModule
    {
        public IDeformerModule Module
        {
            get
            {
                if (module == null) Deserialize();
                return module;
            }
            set => module = value;
        }

        [ShowInInspector] private IDeformerModule module;
        private IDeformer deformer;

        public Type GetLayerType()
        {
            return Type.GetType(type);
        }

        [SerializeField] private string serializedData;
        [SerializeField] private string type;

        public SerializedDeformerModule(IDeformer deformer, Type newLayerType)
        {
            module = Activator.CreateInstance(newLayerType) as IDeformerModule;
            module.Init(deformer);
            Serialize();
        }

        public void Init(IDeformer deformer)
        {
            this.deformer = deformer;
        }

        private void Deserialize()
        {
            Debug.Log(type);
            Type t = GetLayerType();
            module = JsonConvert.DeserializeObject(serializedData, t) as IDeformerModule;
            module?.Init(deformer);
        }

        public void Serialize()
        {
            type = module.GetType().FullName;
            serializedData = JsonConvert.SerializeObject(module);
        }
    }
}
