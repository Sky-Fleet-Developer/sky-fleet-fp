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
        void OnInitComplete();
    }

    public interface IDamagemleBlock : IBlock
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

    public interface ISpecialPorts
    {
        IEnumerable<Port> GetPorts();
    }
    
    public interface IControl : IInteractiveBlock, IUpdatableBlock, ISpecialPorts
    {
        bool IsUnderControl { get; }
        List<ControlAxe> Axes { get; }
        CharacterAttachData GetAttachData();
        void ReadInput();
        void LeaveControl(ICharacterController controller);
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