using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Character.Interaction;
using Core.Misc;
using Core.Structure;
using Core.World;
using UnityEngine;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace Core.Ai
{
    public struct MenaceRef
    {
        public IStructure Structure;
        public IWeaponHandler WeaponHandler;
        public float Dot;
    }
    
    public class MenacesWatcher : MonoBehaviour, IMyInstaller, ITickable
    {
        //[SerializeField] private float menaceRange;
        [Inject] private TickService _tickService;
        private Dictionary<UnitEntity, List<IMenace>> _unitsWithMenaces = new();
        //private Dictionary<IStructure, List<IWeaponHandler>> _weaponDict = new();
        //private Dictionary<IStructure, List<MenaceRef>> _menacesDict = new();
        public int TickRate => 10;

        //public void RegisterMenace(UnitEntity unit, IMenace menace)

        /*private void Start()
        {
            CycleService.OnStructureAdd += OnStructureAdd;
            CycleService.OnStructureRemoved += OnStructureRemoved;
            _tickService.Add(this);
        }

        private void OnDestroy()
        {
            foreach (var structure in _weaponDict.Keys)
            {
                structure.OnBlockAddedToStructureEvent -= OnBlockAdded;
                structure.OnBlockRemovedFromStructureEvent -= OnBlockRemoved;
            }
            _weaponDict.Clear();
            CycleService.OnStructureAdd -= OnStructureAdd;
            CycleService.OnStructureRemoved -= OnStructureRemoved;
        }

        private void OnStructureAdd(IStructure structure)
        {
            _weaponDict.Add(structure, new(structure.GetBlocksByType<IWeaponHandler>()));
            _menacesDict.Add(structure, new());
            structure.OnBlockAddedToStructureEvent += OnBlockAdded;
            structure.OnBlockRemovedFromStructureEvent += OnBlockRemoved;
        }
        private void OnStructureRemoved(IStructure structure)
        {
            _weaponDict.Remove(structure);
            _menacesDict.Remove(structure);
            structure.OnBlockAddedToStructureEvent += OnBlockAdded;
            structure.OnBlockRemovedFromStructureEvent += OnBlockRemoved;
        }
        
        private void OnBlockAdded(IStructure structure, IBlock block)
        {
            if (block is IWeaponHandler weaponHandler)
            {
                _weaponDict[structure].Add(weaponHandler);
            }
        }

        private void OnBlockRemoved(IStructure structure, IBlock block)
        {
            if (block is IWeaponHandler weaponHandler)
            {
                _weaponDict[structure].Remove(weaponHandler);
            }
        }



        private void CheckMenaces(KeyValuePair<IStructure, List<IWeaponHandler>> kv)
        {
            
        }
*/
        
        public void Tick()
        {
           // Parallel.ForEach(_weaponDict, CheckMenaces);
        }
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<MenacesWatcher>().AsSingle();
        }
    }
}