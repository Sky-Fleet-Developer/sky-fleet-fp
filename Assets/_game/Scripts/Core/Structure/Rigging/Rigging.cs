using System;
using System.Collections.Generic;
using Core.Character;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure.Rigging.Control;
using Core.Utilities;
using UnityEngine;

namespace Core.Structure.Rigging
{
    public interface IMass
    {
        float Mass { get; }
    }

    public interface IPowerUser : IBlock, IGraphNode
    {
        void ConsumptionTick();
        void PowerTick();
    }

    public interface IConsumer : IPowerUser
    {
        bool IsWork { get; }
        float Consumption { get; }
        PowerPort Power { get; }
    }

    public interface IFuelUser : IBlock, IGraphNode
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
        void UpdateBlock(int lod);
    }

    public interface IInteractiveBlock : IBlock
    {
        IEnumerable<IInteractiveDevice> GetInteractiveDevices();
        (bool canInteractive, string data) RequestInteractive(ICharacterController character);
        void Interaction(ICharacterController character);
    }

    public interface IInteractiveDevice
    {
        bool EnableInteraction { get; }
        Transform Root { get; }
    }


    public interface IStorage : IGraphNode
    {
        float CurrentAmount { get; }
        float MaximalAmount { get; }
        float MaxInput { get; }
        float MaxOutput { get; }
        float AmountInPort { get; }
        StorageMode Mode { get; set; }
        void PushToPort(float amount);
    }

    public interface IControl : IInteractiveBlock, IUpdatableBlock
    {
        bool IsUnderControl { get; }
        List<ControlAxis> Axes { get; }
        CharacterAttachData GetAttachData();
        void ReadInput();
        void LeaveControl(ICharacterController controller);
    }

    public interface IAimingInterface
    {
        public Vector3 Target { get; }
        public Vector2 Input { get; set; }
        public event Action OnStateChanged;
        public AimingInterfaceState CurrentState { get; }
        public bool SetState(AimingInterfaceState state);
    }

    public enum AimingInterfaceState
    {
        Default = 0,
        FollowTarget = 1,
        Aiming = 2,
    }

    public interface IComputer : IUpdatableBlock, IConsumer
    {
    }

    public interface IGyroscope : IUpdatableBlock, IConsumer
    {
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