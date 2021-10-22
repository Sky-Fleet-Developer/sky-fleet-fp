using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Utilities;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Environment
{
    public class StructureRaycaster : MonoBehaviour, ILoadAtStart
    {
        public static StructureRaycaster Instance;

        [ShowInInspector]
        public Dictionary<IStructure, StructureRayCastingProfile> Profiles =
            new Dictionary<IStructure, StructureRayCastingProfile>();

        public Task Load()
        {
            Instance = this;

            foreach (IStructure structure in StructureUpdateModule.Structures)
            {
                OnRegisterStructure(structure);
            }

            StructureUpdateModule.OnStructureInitialized += OnRegisterStructure;
            StructureUpdateModule.OnStructureDestroy += OnDestroyStructure;

            return Task.CompletedTask;
        }

        private void OnDestroy()
        {
            StructureUpdateModule.OnStructureInitialized -= OnRegisterStructure;
            StructureUpdateModule.OnStructureDestroy -= OnDestroyStructure;
        }

        private void OnRegisterStructure(IStructure structure)
        {
            StructureRayCastingProfile profile = new StructureRayCastingProfile(structure);
            Profiles.Add(structure, profile);
        }

        private void OnDestroyStructure(IStructure structure)
        {
            Profiles.Remove(structure);
        }

        public static bool Cast(Ray ray, bool interactiveCast, float maxDistance, LayerMask layerMask,
            out StructureHit hit)
        {
            foreach (IStructure structure in Instance.Profiles.Keys)
            {
                if (Cast(structure, ray, interactiveCast, maxDistance, layerMask, out hit))
                {
                    return true;
                }
            }

            hit = default;
            return false;
        }

        public static bool Cast(IStructure structure, Ray ray, bool interactiveCast, float maxDistance,
            LayerMask layerMask, out StructureHit hit)
        {
            _globalRay = ray;
            _interactiveCast = interactiveCast;
            hit = new StructureHit();
            _lastRaySpace = null;
            return Instance.Profiles[structure].Cast(maxDistance, layerMask, ref hit);
        }

        private static bool _interactiveCast;
        private static Ray _globalRay;
        private static Ray _localRay;
        private static Transform _lastRaySpace;

        public static void SetRayToSpace(Transform transform)
        {
            if (_lastRaySpace == transform) return;
            _lastRaySpace = transform;
            _localRay = new Ray(transform.InverseTransformPoint(_globalRay.origin),
                transform.InverseTransformDirection(_globalRay.direction));
        }

        [ShowInInspector]
        public class StructureRayCastingProfile
        {
            public Bounds LocalBounds;
            [ShowInInspector] public readonly Transform Transform;
            [ShowInInspector] private readonly IStructure structure;

            [ShowInInspector] public readonly List<BlockRayCastingProfile> Blocks = new List<BlockRayCastingProfile>();

            public StructureRayCastingProfile(IStructure structure)
            {
                this.structure = structure;
                Transform = structure.transform;
                Quaternion rotation = Transform.rotation;

                Transform.rotation = Quaternion.identity;
                LocalBounds = Transform.GetBounds();
                LocalBounds.center = Transform.InverseTransformPoint(LocalBounds.center);

                foreach (IBlock structureBlock in structure.Blocks)
                {
                    BlockRayCastingProfile profile;
                    if (structureBlock is IInteractiveBlock interactiveBlock)
                    {
                        profile = new InteractiveBlockProfile(interactiveBlock);
                    }
                    else
                    {
                        profile = new BlockRayCastingProfile(structureBlock);
                    }

                    Blocks.Add(profile);
                }

                Transform.rotation = rotation;
            }


            public bool Cast(float maxDistance, LayerMask layerMask, ref StructureHit hitInfo)
            {
                SetRayToSpace(Transform);

                if (!LocalBounds.IntersectRay(_localRay, out float distance)) return false;
                if (!(distance < maxDistance)) return false;
                hitInfo.Structure = structure;
                return CastBlocks(maxDistance, layerMask, ref hitInfo);
            }

            public bool CastBlocks(float maxDistance, LayerMask layerMask, ref StructureHit hitInfo)
            {
                foreach (BlockRayCastingProfile block in Blocks)
                {
                    if (block.Cast(maxDistance, layerMask, ref hitInfo)) return true;
                }

                return false;
            }
        }

        [ShowInInspector]
        public class BlockRayCastingProfile
        {
            [ShowInInspector] private Bounds localBounds;
            [ShowInInspector] private readonly Parent parent;
            [ShowInInspector] protected readonly IBlock block;

            public BlockRayCastingProfile(IBlock structureBlock)
            {
                block = structureBlock;
                localBounds = structureBlock.GetBounds();
                parent = structureBlock.Parent;
            }

            public virtual bool Cast(float maxDistance, LayerMask layerMask, ref StructureHit hitInfo)
            {
                SetRayToSpace(parent.Transform);
                if (!localBounds.IntersectRay(_localRay, out float distance)) return false;
                if (!(distance < maxDistance)) return false;
                hitInfo.Block = block;
                hitInfo.HitInBlockBounds = true;
                if (!Physics.Raycast(_localRay, out RaycastHit hit, maxDistance, layerMask)) return false;
                
                hitInfo.RaycastHit = hit;

                return hit.transform == parent.Transform || hit.transform.IsChildOf(parent.Transform);
            }
        }

        [ShowInInspector]
        public class InteractiveBlockProfile : BlockRayCastingProfile
        {
            [ShowInInspector]
            private readonly List<DeviceRayCastingProfile> devices = new List<DeviceRayCastingProfile>();

            [ShowInInspector] private readonly IInteractiveBlock interactiveBlock;

            public InteractiveBlockProfile(IInteractiveBlock interactiveBlock) : base(interactiveBlock)
            {
                this.interactiveBlock = interactiveBlock;
                foreach (IInteractiveDevice interactiveDevice in interactiveBlock.GetInteractiveDevices())
                {
                    DeviceRayCastingProfile profile = new DeviceRayCastingProfile(interactiveDevice);
                    devices.Add(profile);
                }
            }

            public override bool Cast(float maxDistance, LayerMask layerMask, ref StructureHit hitInfo)
            {
                bool preliminaryResult = base.Cast(maxDistance, layerMask, ref hitInfo);
                hitInfo.InteractiveBlock = interactiveBlock;
                if (!_interactiveCast) return preliminaryResult;
                
                if (!hitInfo.HitInBlockBounds) return false;

                float angle = 0;
                DeviceRayCastingProfile selectedDevice = null;
                foreach (DeviceRayCastingProfile device in devices)
                {
                    float a = device.Cast(maxDistance);
                    if (a < angle)
                    {
                        angle = a;
                        selectedDevice = device;
                    }
                }

                hitInfo.Device = selectedDevice.InteractiveDevice;
                return true;
            }
        }

        [ShowInInspector]
        public class DeviceRayCastingProfile
        {
            [ShowInInspector] private Vector3 localCenter;
            [ShowInInspector] private Transform transform;
            [ShowInInspector] public IInteractiveDevice InteractiveDevice;

            public DeviceRayCastingProfile(IInteractiveDevice interactiveDevice)
            {
                InteractiveDevice = interactiveDevice;
                transform = interactiveDevice.Root;
                Bounds bounds = transform.GetBounds();
                localCenter = transform.InverseTransformPoint(bounds.center);
            }

            public float Cast(float maxDistance)
            {
                Vector3 globalCenter = transform.TransformPoint(localCenter);
                Vector3 delta = globalCenter - _globalRay.origin;
                float dot = Vector3.Dot(delta.normalized, _globalRay.direction);
                return Vector3.SqrMagnitude(delta) < maxDistance * maxDistance ? -dot : 1;
            }
        }
    }



    public struct StructureHit
    {
        public IStructure Structure;
        public IBlock Block;
        public bool HitInBlockBounds;
        public IInteractiveBlock InteractiveBlock;
        public IInteractiveDevice Device;
        public RaycastHit RaycastHit;

    }
}
