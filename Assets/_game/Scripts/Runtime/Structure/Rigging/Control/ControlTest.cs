using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
using Core.SessionManager.SaveService;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control
{
    public class ControlTest : Block, IControl
    {
        //public float Durability => durability;
        //public ArmorData Armor => armor;
        public List<ControlAxe> Axes => axes;
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
        [SerializeField] private List<ControlAxe> axes;
        [SerializeField] private List<ControlButton> buttons;
        [SerializeField] private List<ControlToggle> toggles;
        [SerializeField] private List<ControlTrackball> trackballs;

        public CharacterAttachData attachData;
        public CharacterDetachhData detachData;
        [System.NonSerialized, ShowInInspector] public ICharacterController controller;


        IVisibleControlElement[] controlElementsCache;
        private IVisibleControlElement[] GetVisibleControlElement()
        {
            IVisibleControlElement[] ar = new IVisibleControlElement[axes.Count + buttons.Count + toggles.Count + trackballs.Count];
            Array.Copy(axes.ToArray(), ar, axes.Count);
            Array.Copy(buttons.ToArray(), 0, ar, axes.Count, buttons.Count);
            Array.Copy(toggles.ToArray(), 0, ar, axes.Count + buttons.Count, toggles.Count);
            Array.Copy(trackballs.ToArray(), 0, ar, axes.Count + buttons.Count + toggles.Count, trackballs.Count);
            return ar;
        }

        public override void OnInitComplete()
        {
            controlElementsCache = GetVisibleControlElement();
            Array.ForEach(controlElementsCache, x =>
            {
                if (x.Device != null)
                {
                    x.Device.Init(Structure, this, name + x.PortAbstact.Guid);
                }
            });
        }

        public void ReadInput()
        {
            Array.ForEach(controlElementsCache, x =>
            {
                x.Tick();
            });
        }

        public IEnumerable<PortPointer> GetPorts()
        {
            if (controlElementsCache == null)
            {
                controlElementsCache = GetVisibleControlElement();
            }
            return controlElementsCache.Select(x => new PortPointer(this, x.PortAbstact));
        }

        public (bool canInteractive, string data) RequestInteractive(ICharacterController character)
        {
            if (isUnderControl) return (false, GameData.Data.controlFailText);

            return (true, string.Empty);
        }

        public void Interaction(ICharacterController character)
        {
            if (RequestInteractive(character).canInteractive)
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

        public void UpdateBlock(int lod)
        {
            if (lod != 0) return;

            Array.ForEach(controlElementsCache, x =>
            {
                if (x.Device != null)
                {
                    x.Device.UpdateDevice();
                }
            });
        }
    }
}
