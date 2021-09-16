using System.Collections;
using System.Collections.Generic;
using Character.Control;
using Scripts.Utility;
using Structure.Rigging.Control;
using Structure.Wires;
using UnityEngine;

namespace Structure.Rigging
{
    public interface IBlock
    {
        Transform transform { get; }
        // ReSharper disable once InconsistentNaming
        Vector3 localPosition { get; }
        Parent Parent { get; }
        IStructure Structure { get; }
        string Guid { get; }
        string MountingType { get; }

        void InitBlock(IStructure structure, Parent parent);
    }

    public interface IDamagemleBlock : IBlock
    {
        float Durability { get; }
        ArmorData Armor { get; }
    }

    public interface IInteractibleBlock : IBlock
    {
        (bool canInteractive, string data) RequestInteractive(ICharacterController character);
        void Interaction(ICharacterController character);
    }

    public interface ISpecialPorts
    {
        IEnumerable<Port> GetPorts();
    }
    
    public interface IControl : IInteractibleBlock, ISpecialPorts
    {
        bool IsUnderControl { get; }
        List<ControlAxe> Axes { get; }
        CharacterControlData GetAttachData();
        void ReadInput();
    }

    [System.Serializable]
    public struct CharacterControlData
    {
        public Transform anchor;
        public bool attachAndLock;
        public DOTweenTransition transition;
    }

    public interface IPowerUser : IBlock
    {
        void PowerTick();
    }
    
    public interface IFuelUser : IBlock
    {
        void FuelTick();
    }

    public interface IHydrogenStorage : IFuelUser
    {
        float MaximalVolume { get; }
        float CurrentVolume { get; }
    }

    public interface IForceUser : IBlock
    {
        void ApplyForce();
    }

    public interface IJetBlock : IFuelUser, IForceUser
    {
        float MaximalThurst { get; }
    }

    [System.Serializable]
    public struct ArmorData
    {
        public float tickness;
        public float quality;
    }
}
