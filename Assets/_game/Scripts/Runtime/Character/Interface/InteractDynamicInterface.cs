using Core.Character;
using Core.Character.Interface;
using Core.Patterns.State;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.Character.Interface
{
    public class InteractDynamicInterface : MonoBehaviour, IFirstPersonInterface
    {
        [SerializeField] private Image pointMark;
        [SerializeField] private Image path;
        [SerializeField] private Gradient pathGradientPerTension;
        [SerializeField] private float tensionMul;
        private RectTransform _rectTransform;
        private FirstPersonController.InteractWithDynamicObjectState _data;
        private Camera _mainCamera;
        private FirstPersonInterfaceInstaller _master;

        public void Init(FirstPersonInterfaceInstaller master)
        {
            _master = master;
        }

        public bool IsMatch(IState state) => state is FirstPersonController.InteractWithDynamicObjectState;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _mainCamera = Camera.main;
            _data = (FirstPersonController.InteractWithDynamicObjectState)_master.TargetState;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Update()
        {
            Vector2 screenStart = _mainCamera.WorldToScreenPoint(_data.CurrentPoint);
            Vector2 screenEnd = _mainCamera.WorldToScreenPoint(_data.WantedPoint);
            Vector2 delta = screenEnd - screenStart;
            path.rectTransform.anchoredPosition = screenStart;
            path.transform.localEulerAngles = Vector3.forward * (Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            path.rectTransform.sizeDelta = new Vector2(delta.magnitude, path.rectTransform.sizeDelta.y);
            pointMark.rectTransform.anchoredPosition = screenStart;
            path.color = pathGradientPerTension.Evaluate(_data.PullTension / tensionMul);
        }
    }
}