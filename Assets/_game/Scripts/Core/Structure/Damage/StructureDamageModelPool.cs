using System.Collections.Generic;
using System.Linq;
using Core.Weapon;
using UnityEngine;

namespace Core.Structure.Damage
{
    public class StructureDamageModelPool
    {
        private List<StructureDamageModel> _instances;
        private int _bisy;
        private Vector3 _initialPoint;
        private Vector3 _offset;
        private ProjectileHandler _projectileHandler;

        public StructureDamageModelPool(int initialSize, ProjectileHandler handler)
        {
            _projectileHandler = handler;
            _instances = new List<StructureDamageModel>(initialSize);
        }
        
        public void Init(IStructure source, Vector3 initialPoint, Vector3 offset)
        {
            _offset = offset;
            _initialPoint = initialPoint;
            Add(source);
            for (int i = 1; i < _instances.Capacity; i++)
            {
                var instance = Object.Instantiate(source.transform, initialPoint + offset * i, Quaternion.identity, source.transform.parent);
                instance.name = source.transform.name[..^3] + "(" + i + ")";
                Add(instance.GetComponent<IStructure>());
            }
        }

        private void Add(IStructure instance)
        {
            StructureDamageModel model = new StructureDamageModel();
            model.Root = instance.transform;
            model.Parents = instance.Parents.Select(x => x.Transform).ToArray();
            model.Root.InitAsDamageModel(_projectileHandler);
            _instances.Add(model);
        }

        public StructureDamageModel Get()
        {
            if (_bisy >= _instances.Count)
            {
                Expand();
            }
            return _instances[_bisy++];
        }

        public void Reset()
        {
            _bisy = 0;
        }

        private void Expand()
        {
            int count = _instances.Count;
            _instances.Capacity = count * 2;
            var source = _instances[0];
            for (int i = 0; i < count; i++)
            {
                var instance = Object.Instantiate(source.Root.transform, _initialPoint + _offset * i, Quaternion.identity, source.Root.parent);
                Add(instance.GetComponent<IStructure>());
            }
        }
    }
}