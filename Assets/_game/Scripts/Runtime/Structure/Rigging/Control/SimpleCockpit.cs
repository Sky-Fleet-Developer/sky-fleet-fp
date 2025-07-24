using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.Data;
using Core.Graph;
using Core.Graph.Wires;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Core.Structure.Rigging.Control.Attributes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class SimpleCockpit : BlockWithNode, IControl
    {
        //public float Durability => durability;
        //public ArmorData Armor => armor;
        public List<ControlAxis> Axes => axes;
        public List<ControlButton> Buttons => buttons;
        public List<ControlToggle> Toggles => toggles;
        public List<ControlTrackball> Trackballs => trackballs;

        public bool IsUnderControl => isUnderControl;

        //[SerializeField] private float durability;
        //[SerializeField]  private ArmorData armor;

        [PlayerProperty]
        public float SomeProperty
        {
            get => 1f;
            set => Debug.Log("Set property: " + value);
        }

        [ReadOnly, ShowInInspector] private bool isUnderControl;
        public List<ControlAxis> axes;
        public List<ControlButton> buttons;
        public List<ControlToggle> toggles;
        public List<ControlTrackball> trackballs;
        private IDevice[] devices;

        public CharacterAttachData attachData;
        public CharacterDetachhData detachData;
        [System.NonSerialized, ShowInInspector] public ICharacterController controller;
        private List<IControlElement> controlElementsCache;
        
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

       /* public IEnumerable<PortPointer> GetPorts()
        {
            if (controlElementsCache == null)
            {
                CollectControlElements();
            }
            return controlElementsCache.Select(x => new PortPointer(this, x.GetPort()));
        }*/

        IEnumerable<IInteractiveDevice> IInteractiveBlock.GetInteractiveDevices()
        {
            return controlElementsCache;
        }

        (bool canInteract, string data) IInteractiveObject.RequestInteractive(ICharacterController character)
        {
            if (isUnderControl) return (false, GameData.Data.controlFailText);

            return (true, string.Empty);
        }

        void IInteractiveBlock.Interaction(ICharacterController character)
        {
            if (((IInteractiveBlock)this).RequestInteractive(character).canInteract)
            {
                StartCoroutine(InteractionRoutine(character));
            }
        }

        private IEnumerator InteractionRoutine(ICharacterController character)
        {
            yield return character.AttachToControl(this);
            isUnderControl = true;
            controller = character;
        }

        public void LeaveControl(ICharacterController character)
        {
            if (isUnderControl && character == controller)
            {
                StartCoroutine(LeaveControlRoutine(character));
            }
        }

        private IEnumerator LeaveControlRoutine(ICharacterController character)
        {
            yield return character.LeaveControl(detachData);
            isUnderControl = false;
            controller = null;
        }

        public CharacterAttachData GetAttachData()
        {
            return attachData;
        }

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
