using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Character;
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
        public bool IsUnderControl => isUnderControl;

        //[SerializeField] private float durability;
        //[SerializeField]  private ArmorData armor;

        [PlayerProperty] public float SomeProperty
        {
            get => 1f;
            set => Debug.Log("Set property: " + value);
        }
        
        [ReadOnly, ShowInInspector] private bool isUnderControl;
        [SerializeField] private List<ControlAxe> axes;

        public CharacterAttachData attachData;
        public CharacterDetachhData detachData;
        [System.NonSerialized, ShowInInspector] public ICharacterController controller;

        public override void OnInitComplete()
        {
            foreach (var controlAxe in axes)
            {
                if (controlAxe.device != null)
                {
                    controlAxe.device.Init(Structure, this, controlAxe.port.Guid);
                }
            }
        }

        public void ReadInput()
        {
            foreach (var axe in axes)
            {
                axe.Tick();
            }
        }

        public IEnumerable<Port> GetPorts()
        {
            return axes.Select(x => x.port);
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

        public void UpdateBlock()
        {
            foreach (var controlAxe in axes)
            {
                if (controlAxe.device != null)
                {
                    controlAxe.device.UpdateDevice();
                }
            }
        }
    }
}