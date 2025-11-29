using Core.Character;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Structure.Rigging
{
    public interface IInteractiveObject : IEventSystemHandler
    {
        void Interact(InteractEventData eventData);
    }
    
    /*public interface IInteractiveProbeObject : IEventSystemHandler
    {
        void TakeProbe(InteractEventData eventData);
    }*/

    public enum KeyModifier
    {
        Hold = 0,
        Up = 1,
        Down = 2
    }

    public enum InteractLevel
    {
        /// <summary>
        /// Special key to interact with specific object
        /// </summary>
        Primary = 1,
        /// <summary>
        /// Drag and drop key (mouse 0 or etc.)
        /// </summary>
        Secondary = 2
    }

    public class InteractEventData : BaseEventData
    {
        private ICharacterController _controller;
        private InteractLevel _level;
        private KeyModifier _keyModifier;
        private float _pressDownTime;
        private Vector2 _mouseDelta;
        public ICharacterController Controller => _controller;
        public InteractLevel Level => _level;
        public KeyModifier KeyModifier => _keyModifier;
        public bool IsLongPress => Time.time - _pressDownTime > 0.5f;
        public Vector2 MouseDelta => _mouseDelta;
        
        public InteractEventData(ICharacterController controller, InteractLevel interactLevel, KeyModifier keyModifier,
            float pressDownTime, Vector2 mouseDelta,
            EventSystem eventSystem) : base(eventSystem)
        {
            _mouseDelta = mouseDelta;
            _pressDownTime = pressDownTime;
            _keyModifier = keyModifier;
            _level = interactLevel;
            _controller = controller;
        }
    }
}