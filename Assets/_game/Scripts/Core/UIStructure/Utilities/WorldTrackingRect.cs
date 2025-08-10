using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public class WorldTrackingRect : MonoBehaviour
    {
        public enum TrackingMode
        {
            Update,
            LateUpdate,
            FixedUpdate,
        }
        [SerializeField] private Transform target;
        [SerializeField] private TrackingMode trackingMode;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        public void SetTrackingObject(Transform target)
        {
            this.target = target;
        }

        private void Update()
        {
            if (trackingMode == TrackingMode.Update)
            {
                Track();
            }
        }
        private void LateUpdate()
        {
            if (trackingMode == TrackingMode.LateUpdate)
            {
                Track();
            }
        }
        private void FixedUpdate()
        {
            if (trackingMode == TrackingMode.FixedUpdate)
            {
                Track();
            }
        }


        private void Track()
        {
            Vector3 position = _mainCamera.WorldToScreenPoint(target.position);
            transform.position = position;
            gameObject.SetActive(position.z > 0);
        }
    }
}