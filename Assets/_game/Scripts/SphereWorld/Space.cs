using System;
using System.Collections.Generic;
using Core.Utilities;
using Core.View;
using UnityEngine;
using Zenject;

namespace SphereWorld
{
    public class Space
    {
        private Anchor _currentAnchor;
        private readonly float _zeroHeight;
        private CachedRequest<Vector3> _globalAnchorPosition;
        private float _degreeLengthMeters;
        public float ZeroHeight => _zeroHeight;
        public Anchor Anchor => _currentAnchor;

        [Inject] private ViewSettings _viewSettings;
        public Space(float zeroHeight)
        {
            _zeroHeight = zeroHeight;
            _globalAnchorPosition = new CachedRequest<Vector3>(Time.deltaTime, () => _currentAnchor!.Polar.ToGlobal(_zeroHeight));
            _degreeLengthMeters = (float)(2d * Math.PI * (double)zeroHeight * 2.7777777777777777d); // *1000m/360deg
        }

        public void InjectAnchor(Anchor anchor)
        {
            _currentAnchor = anchor;
        }

        public bool IsVisible(Polar value)
        {
            return (_globalAnchorPosition.Value - value.ToGlobal(_zeroHeight)).sqrMagnitude <
                   _viewSettings.viewRadius * _viewSettings.viewRadius;
        }
        
        public Vector3 GetOffset(Polar value)
        {
            Polar diff = value - _currentAnchor.Polar;
            Vector3 asVector = diff;
            asVector.x *= _degreeLengthMeters;
            asVector.z *= _degreeLengthMeters;
            asVector.y *= 1000;
            return asVector;
        }
        
        public Vector3 GetOffsetSafe(Polar value)
        {
            Polar diff = value - _currentAnchor.Polar;
            diff = diff.ClampCircle();
            Vector3 asVector = diff;
            asVector.x *= _degreeLengthMeters;
            asVector.z *= _degreeLengthMeters;
            asVector.y *= 1000;
            return asVector;
        }

        /*public Vector3 SceneToGlobalPosition(Vector3 value)
        {
            //return _currentAnchor.globalPosition + CalculateGlobalOffset(value);
            
        }

        private Vector3 CalculateGlobalOffset(Vector3 offset)
        {
            
        }*/

        /// <returns>Vector3(latitude, height, longitude)</returns>
        public static Vector3 RectToSpherical(Vector3 rectCoords) 
        {
            Vector3 result = Vector3.zero;
            result.y = rectCoords.magnitude;

            result.x = Mathf.Acos(rectCoords.y / result.y);
            result.z = Mathf.Atan2(rectCoords.z, rectCoords.x);
            return result;
        }
        
        /// <param name="value">Vector3(latitude, height, longitude)</param>
        public static Vector3 SphericalToRect(Vector3 value)
        {
            return new Vector3(
                value.y * Mathf.Sin(value.x) * Mathf.Cos(value.z),
                value.y * Mathf.Cos(value.x),
                value.y * Mathf.Sin(value.x) * Mathf.Sin(value.z));
        }
    }
}