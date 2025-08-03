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
    public class SimpleCockpit : BlockWithNode, ICharacterInterface, IUpdatableBlock
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
        public bool RequestInteractive(ICharacterController character, out string data)
        {
            if (isUnderControl)
            {
                data = GameData.Data.controlFailText;
                return false;
            }
            data = string.Empty;
            if (!canAttachController)
            {
                return false;
            }
            return true;
        }

        void IInteractiveBlock.Interaction(ICharacterController character)
        {
            if (((IInteractiveBlock)this).RequestInteractive(character, out _))
            {
                character.AttachToControl(this);
            }
        }

        void ICharacterInterface.OnCharacterEnter(ICharacterController character)
        {
            isUnderControl = true;
            controller = character;
        }

        void ICharacterInterface.OnCharacterLeave(ICharacterController character)
        {
            isUnderControl = false;
            controller = null;
        }

        public CharacterAttachData GetAttachData() => attachData;
        public CharacterDetachData GetDetachData() => detachData;


        public virtual void UpdateBlock(int lod)
        {
            if (lod != 0) return;

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

        public bool EnableInteraction => IsActive;
        public Transform Root { get; }
    }
}
