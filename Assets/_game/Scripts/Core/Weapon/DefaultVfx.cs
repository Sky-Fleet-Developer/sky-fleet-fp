using Core.Utilities;
using UnityEngine;

namespace Core.Weapon
{
    public class DefaultVfx : MonoBehaviour, IVfx
    {
        public bool NeedManualReturn => false;
        [SerializeField] private bool autoReturn;
        private ParticleSystem[] particles;
        private TrailRenderer[] trails;
        private bool _needReturn;
        private int _returnCounter;
        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }
        private void Awake()
        {
            particles = GetComponentsInChildren<ParticleSystem>();
            trails = GetComponentsInChildren<TrailRenderer>();
        }

        public void Play()
        {
            _needReturn = false;
            foreach (var particle in particles)
            {
                particle.Play();
            }

            foreach (var trailRenderer in trails)
            {
                trailRenderer.emitting = true;
            }
        }

        private void Update()
        {
            if (autoReturn || _needReturn)
            {
                bool canRelease = true;
                foreach (var trailRenderer in trails)
                {
                    if (trailRenderer.positionCount != 0)
                    {
                        canRelease = false;
                        break;
                    }
                }
                foreach (var particle in particles)
                {
                    if (particle.isPlaying)
                    {
                        canRelease = false;
                        break;
                    }
                }

                if (canRelease)
                {
                    _returnCounter++;
                    if (_returnCounter > 10)
                    {
                        DynamicPool.Instance.Return(this);
                    }
                }
                else
                {
                    _returnCounter = 0;
                }
            }
        }

        public void Stop()
        {
            foreach (var particle in particles)
            {
                particle.Stop();
            }

            foreach (var trailRenderer in trails)
            {
                trailRenderer.emitting = false;
            }

            _needReturn = true;
        }
    }
}