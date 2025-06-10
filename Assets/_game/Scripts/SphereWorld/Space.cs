using Core.Utilities;
using UnityEngine;

namespace SphereWorld
{
    public class Space
    {
        private Anchor _currentAnchor;
        private float _zeroHeight;
        private CachedRequest<Vector3> _rectangleAnchorCoordinates;

        public Space(float zeroHeight)
        {
            _zeroHeight = zeroHeight;
            /*_rectangleAnchorCoordinates = new CachedRequest<Vector3>(Time.deltaTime, () =>
            {
                
            });*/
        }

        public void InjectAnchor(Anchor anchor)
        {
            _currentAnchor = anchor;
        }

        /*public Vector3 SceneToGlobalPosition(Vector3 value)
        {
            //return _currentAnchor.globalPosition + CalculateGlobalOffset(value);
            
        }

        private Vector3 CalculateGlobalOffset(Vector3 offset)
        {
            
        }*/

        public static Vector3 RectToSpherical(Vector3 rectCoords) 
        {
            Vector3 result = Vector3.zero;
            result.y = rectCoords.magnitude;

            result.x = Mathf.Acos(rectCoords.y / result.y);
            result.z = Mathf.Atan2(rectCoords.z, rectCoords.x);
            return result;
        }

        public static Vector3 SphericalToRect(Vector3 value)
        {
            return new Vector3(
                value.y * Mathf.Sin(value.x) * Mathf.Cos(value.z),
                value.y * Mathf.Cos(value.x),
                value.y * Mathf.Sin(value.x) * Mathf.Sin(value.z));
        }
    }
}