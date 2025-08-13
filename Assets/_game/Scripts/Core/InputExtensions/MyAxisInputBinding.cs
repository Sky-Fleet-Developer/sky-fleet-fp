using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.Layouts;

namespace Core.InputExtensions
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class MyAxisInputBinding : AxisComposite
    {
                
#if UNITY_EDITOR
        static MyAxisInputBinding()
        {
            Initialize();
        }
#endif
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterBindingComposite<MyAxisInputBinding>("MyAxis");
        }
        public enum AxisType
        {
            Absolute,
            Relative
        }
        public AxisType axisType = AxisType.Absolute;
        public float minOutputValue = -1; 
        public float maxOutputValue = 1;
        public float increaseSensitivity = 1; 
        public float decreaseSensitivity = 1;
        public float moveToZeroSensitivityModifier = 1;
        private float _value;
        
        public override float ReadValue(ref InputBindingCompositeContext context)
        {
            var value = base.ReadValue(ref context);
            switch (axisType)
            {
                case AxisType.Relative:
                    bool isMoveToZero = _value > 0 != value > 0;
                    _value += value * Time.deltaTime * (value > 0 ? increaseSensitivity : decreaseSensitivity) *
                              (isMoveToZero ? moveToZeroSensitivityModifier : 1);
                    _value = Mathf.Clamp(_value, minOutputValue, maxOutputValue);
                    return _value;
                default:
                    isMoveToZero = value * value < _value * _value && _value > 0 == value > 0;
                    _value = Mathf.MoveTowards(_value, value,Time.deltaTime * (value > 0 ? increaseSensitivity : decreaseSensitivity) *
                                               (isMoveToZero ? moveToZeroSensitivityModifier : 1));
                    _value = Mathf.Clamp(_value, minOutputValue, maxOutputValue);
                    return _value;
            }
        }
    }
}