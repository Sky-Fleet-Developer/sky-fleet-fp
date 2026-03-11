using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.Character.Interaction;
using Core.Data;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class SimpleCockpit : BlockWithNode, IDriveInterface, IUpdatableBlock, IInteractiveObject
    {
        public int GetAttachedControllersCount => isUnderControl ? 1 : 0;

        [ReadOnly, ShowInInspector] private bool isUnderControl;
        public List<ControlAxis> axes;
        public List<ControlButton> buttons;
        public List<ControlToggle> toggles;
        public List<ControlTrackball> trackballs;
        private IDevice[] devices;

        public CharacterAttachData attachData;
        public CharacterDetachData detachData;
        [SerializeField] private bool canAttachController = true;
        [System.NonSerialized, ShowInInspector] public ICharacterController controller;
        private List<IControlElement> controlElementsCache;
        public List<IDeviceWithPort> singleDevices = new ();
        
        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            devices = GetComponentsInChildren<IDevice>();
            structure.OnInitComplete.Subscribe(OnInitComplete);
        }

        private void OnInitComplete()
        {
            CollectControlElements();
            foreach (IDevice device in devices)
            {
                device.Init(Graph, this);
            }

            foreach (IControlElement controlElement in controlElementsCache)
            {
                controlElement.Init(Graph, this);
            }
            singleDevices = devices.OfType<IDeviceWithPort>().Where(x => controlElementsCache.Count(v => v.Device == x) == 0).ToList();
        }
        
        private void CollectControlElements()
        {
            controlElementsCache = new List<IControlElement>();
            controlElementsCache.AddRange(axes);
            controlElementsCache.AddRange(buttons);
            controlElementsCache.AddRange(toggles);
            controlElementsCache.AddRange(trackballs);
        }


        public void ReadInput()
        {
            foreach (IControlElement controlElement in controlElementsCache)
            {
                controlElement.Tick();
            }
        }
        public void Interact(InteractEventData data)
        {
            if (isUnderControl || data.used || !canAttachController || data.Level != InteractLevel.Primary)
            {
                return;
            }
            
            data.Controller.EnterHandler(this);
            data.Use();
        }
        
        void IDriveInterface.OnCharacterEnter(ICharacterController character)
        {
            isUnderControl = true;
            controller = character;
            foreach (IControlElement controlElement in controlElementsCache)
            {
                controlElement.Enable();
            }
        }

        void IDriveInterface.OnCharacterLeave(ICharacterController character)
        {
            isUnderControl = false;
            controller = null;
            foreach (IControlElement controlElement in controlElementsCache)
            {
                controlElement.Disable();
            }
        }

        public CharacterAttachData GetAttachData() => attachData;
        public CharacterDetachData GetDetachData() => detachData;


        public virtual void UpdateBlock()
        {
            foreach (IDevice device in devices)
            {
                device.UpdateDevice();
            }
        }
        
        private void OnGUI()
        {
            if (IsActive && isUnderControl && Structure is IDynamicStructure dynamic)
            {
                GUILayout.Label($"<size=25>Speed: {dynamic.Velocity.magnitude * 3.6f : 000.0}km/h</size>");
            }
        }

        public float PitchAxis { get => axes[0].Value; set => axes[0].SetValue(value); }
        public float RollAxis { get => axes[1].Value; set => axes[1].SetValue(value); }
        public float YawAxis { get => axes[2].Value; set => axes[2].SetValue(value); }
        public float ThrustAxis { get => axes[3].Value; set => axes[3].SetValue(value); }
        public float SupportsPowerAxis { get => axes[4].Value; set => axes[4].SetValue(value); }
        public void ResetControls()
        {
            PitchAxis = 0;
            RollAxis = 0;
            YawAxis = 0;
            ThrustAxis = 0;
        }
    }
}
