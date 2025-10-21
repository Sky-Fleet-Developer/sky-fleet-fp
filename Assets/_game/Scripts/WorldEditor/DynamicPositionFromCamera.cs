using Core.World;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    public class DynamicPositionFromCamera : IDynamicPositionProvider
    {
        private Vector3 _offset;
        public Vector3 WorldPosition => Camera.transform.position + _offset;
        public Vector3 SpacePosition => Camera.transform.position;
        public Vector3 StoredVelocity => Vector3.zero;
        private SceneView _sceneView;
        public bool IsInitialized => EditorWindow.HasOpenInstances<SceneView>();
        private SceneView SceneView
        {
            get
            {
                if (_sceneView == null)
                {
                    if (!IsInitialized)
                    {
                        return null;
                    }
                    _sceneView = EditorWindow.GetWindow<SceneView>();
                }
                return _sceneView;
            }
        }
        private Camera Camera => SceneView.camera;

        public Vector3 GetPredictedWorldPosition(float time)
        {
            return WorldPosition;
        }
    }
}