using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Character.Control;
using Management;
using Sirenix.OdinInspector;
using Structure.Wires;
using UnityEngine;

namespace Structure.Rigging.Control
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

        public CharacterControlData attachData;
        [System.NonSerialized, ShowInInspector] public ICharacterController controller;

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
        
        
        public CharacterControlData GetAttachData()
        {
            return attachData;
        }
    }
}
