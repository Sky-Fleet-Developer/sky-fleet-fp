using Core.World;
using UnityEditor;
using UnityEngine;

namespace WorldEditor
{
    public class DynamicPositionFromCamera : IDynamicPositionProvider
    {
        public Vector3 WorldPosition => SceneView.pivot - WorldOffset.Offset;
        public Vector3 SpacePosition => SceneView.pivot;
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

        public Vector3 GetPredictedWorldPosition(float time)
        {
            return WorldPosition;
        }
    }
}