using System;
using System.Collections;
using System.Linq;
using Core.World;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Zenject;

namespace Runtime.Environment.MacroShading
{
    public class MacroShadingProcessor : MonoBehaviour
    {
        [SerializeField] private float updateRate = 1f;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private Light sun;
        [SerializeField] private CustomPassVolume customPassVolume;
        [SerializeField] private AnimationCurve intensityCurve;
        
        [Inject(Id = "Player")]
        private IDynamicPositionProvider _player;
        private float _lastUpdate;
        private CalculateMacroShading _macroShadingPass;
        private float _value;

        private void Start()
        {
            _macroShadingPass = customPassVolume.customPasses.OfType<CalculateMacroShading>().First();
            renderCamera.aspect = 1f;
        }

        private void Update()
        {
            if (Time.time - _lastUpdate > updateRate)
            {
                _lastUpdate = Time.time;
                renderCamera.transform.rotation = sun.transform.rotation;
                renderCamera.transform.position = _player.SpacePosition - renderCamera.transform.forward * renderCamera.farClipPlane;
                renderCamera.Render();
                //sun.intensity = ;
            }

            _value = Mathf.MoveTowards(_value, _macroShadingPass.ResultLightness, Time.deltaTime * 0.15f);
            sun.intensity = intensityCurve.Evaluate(_value);
        }

        private void OnGUI()
        {
            GUILayout.Space(150);
            GUILayout.HorizontalSlider(_value, 0f, 1f, GUILayout.Width(500));
        }
    }
}