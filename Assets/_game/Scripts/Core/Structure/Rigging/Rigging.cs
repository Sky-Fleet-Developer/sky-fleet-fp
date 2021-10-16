using System.Collections.Generic;
using Core.Character;
using Core.Structure.Rigging.Control;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IMass
    {
        float Mass { get; }
    }

    public interface IBlock : ITablePrefab, IMass
    {
        // ReSharper disable once InconsistentNaming
        Vector3 localPosition { get; }
        Parent Parent { get; }
        IStructure Structure { get; }
        string MountingType { get; }

        void InitBlock(IStructure structure, Parent parent);
        void OnInitComplete();

        string Save();
        void Load(string value);
    }
    
    public interface IPowerUser : IBlock
    {
        void ConsumptionTick();
        void PowerTick();

    }
    
    public interface IFuelUser : IBlock
    {
        void FuelTick();
    }
    
    public interface IForceUser : IBlock
    {
        void ApplyForce();
    }

    public interface IDamagebleBlock : IBlock
    {
        float Durability { get; }
        ArmorData Armor { get; }
    }

    public interface IUpdatableBlock : IBlock
    {
        void UpdateBlock();
    }

    public interface IInteractiveBlock : IBlock
    {
        (bool canInteractive, string data) RequestInteractive(ICharacterController character);
        void Interaction(ICharacterController character);
    }

    public interface IControlable
    { 
        
    }


    public interface IStorage
    {
        float CurrentAmount { get; }
        float MaximalAmount { get; }
        float MaxInput { get; }
        float MaxOutput { get; }
        float AmountInPort { get; }
        StorageMode Mode { get; set; }
        void PushToPort(float amount);
    }
    
    public interface ISpecialPorts
    {
        IEnumerable<PortPointer> GetPorts();
    }
    
    public interface IControl : IInteractiveBlock, IUpdatableBlock, ISpecialPorts
    {
        bool IsUnderControl { get; }
        List<ControlAxe> Axes { get; }
        CharacterAttachData GetAttachData();
        void ReadInput();
        void LeaveControl(ICharacterController controller);
    }
    
    public interface IFuelPowerGenerator : IFuelUser, IPowerUser
    {
        float MaximalOutput { get; }
        float FuelConsumption { get; }
        float MaxFuelConsumption { get; }
        float CurrentFuelConsumption { get; }
        float CurrentPowerUsage { get; }
    }

    public interface IJet : IFuelUser, IForceUser
    {
        float MaximalThrust { get; }
        float CurrentThrust { get; }
    }

    public interface ISupport : IPowerUser, IForceUser
    {
        
    }

    public interface ITank : IStorage, IFuelUser
    {
    }

    public enum StorageMode
    {
        Auto = 0,
        Pull = 1,
        Push = 2
    }


    [System.Serializable]
    public struct CharacterAttachData
    {
        public Transform anchor;
        public bool attachAndLock;
        public DOTweenTransition transition;
    }
    [System.Serializable]
    public struct CharacterDetachhData
    {
        public Transform anchor;
        public DOTweenTransition transition;
    }
    




    [System.Serializable]
    public struct ArmorData
    {
        public float tickness;
        public float quality;
    }
}
