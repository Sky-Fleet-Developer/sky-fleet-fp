using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Structure.Damage;
using Core.Structure.Rigging;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using Zenject;
using Random = UnityEngine.Random;

namespace Core.Weapon
{
    [BurstCompile]
    public class ProjectileHandler : MonoBehaviour, IBindMe
    {
        [SerializeField] private ProjectileSettings projectileSettings;
        [SerializeField] private float minSpatialLength = 3f;
        [SerializeField] private bool drawQueries;
        [SerializeField] private float returnImpulseMultiplier = 2f;
        [Inject] private ItemsTable _itemsTable;
        [Inject] private StructureDamageProfileHub _structureDamageProfileHub;
        private SlotMap<ProjectileInstance> _projectiles = new(512);
        private List<StructureRawHit> _structureHitsCacheA = new(32);
        private List<StructureRawHit> _structureHitsCacheB = new(32);
        private List<StructureRawHit> _structureHitsCacheOriginal = new(32);
        private Dictionary<int, ArmorData> _armorMap = new(128);
        private Dictionary<int, IDamagable> _damagableMap = new(128);
        private Dictionary<StructureDamageModelLink, (int, int)> _structureHitsMap = new(); // StructureDamageModelLink, (startIndex, count)
        //public event Action<int, Vector3, Vector3> OnProjectileWaterInteraction;
        private bool _hitsCacheReverse;
        private List<StructureRawHit> HitsCache => _hitsCacheReverse ? _structureHitsCacheB : _structureHitsCacheA;
        public event Action<ProjectileInstance> OnProjectileAdded;
        public event Action<SmKey> OnProjectileRemoved;
        public event Action OnPostUpdate;



        private enum StructureHitResult
        {
            Mess = 0,
            Penetrated = 1,
            Stacked = 2,
        }
        private class StructureRawHit
        {
            public StructureDamageModelLink ModelLink;
            public ProjectileInstance Projectile;
            public StructureHitResult Result;
            public Vector3 ActualPosition;
            public Vector3 RemainingTravel;

            public StructureRawHit(StructureDamageModelLink damageModelLink, ProjectileInstance projectile, float fixedDeltaTime)
            {
                ModelLink = damageModelLink;
                Projectile = projectile;
                RemainingTravel = projectile.Velocity * fixedDeltaTime;
                ActualPosition = projectile.PreviousPosition;
                Result = StructureHitResult.Mess;
            }

            public void StepTo(Vector3 position)
            {
                Vector3 delta = position - ActualPosition;
                Debug.DrawLine(ActualPosition, position, Color.green * 0.5f, 5);
                Debug.DrawRay(ActualPosition, Vector3.up * 0.2f, Color.green * 0.5f, 5);
                Debug.DrawRay(position, Vector3.Cross(delta, Vector3.up).normalized * 0.2f, Color.green, 5);
                RemainingTravel -= delta;
                ActualPosition = position;
            }
        }
        
        public IReadOnlySlotMap<ProjectileInstance> Projectiles => _projectiles;
        
        public bool PullTrigger(IKineticWeapon weapon, ItemInstance shell, out Vector3 returnImpulse)
        {
            var weaponData = _itemsTable.GetKineticWeapon(weapon.SourceItem.Sign.Id);
            var shellData = _itemsTable.GetShell(shell.Sign.Id);
            Vector3 spread = Random.insideUnitSphere * (weaponData.spread * 0.01f);
            float speed = weaponData.impulse / shell.Sign.GetSingleMass();

            Vector3 fwd = weapon.Muzzle.forward + spread;
            ProjectileInstance instance = new ProjectileInstance(weapon.Muzzle.position, fwd, weapon.Velocity, speed, shellData);
            AddProjectile(instance);
            OnProjectileAdded?.Invoke(instance);
            returnImpulse = fwd * (-weaponData.impulse * returnImpulseMultiplier);
            return true;
        }
        
        public void RegisterArmor(int instanceId, ArmorData armorData)
        {
            _armorMap[instanceId] = armorData;
        }

        public void UnregisterArmor(int instanceId)
        {
            _armorMap.Remove(instanceId);
        }

        public void RegisterDamagable(int instanceId, IDamagable damagable)
        {
            _damagableMap[instanceId] = damagable;
        }

        public void UnregisterDamagable(int instanceId)
        {
            _damagableMap.Remove(instanceId);
        }
        
        private void FixedUpdate()
        {
            //if (c > 0)
            //{
            //    Debug.Log($"Hit armor: {c} times this frame");
            //    c = 0;
            //}
            Profiler.BeginSample("ProjectileHandler.StepAndRemove");

            foreach (var projectile in _projectiles.GetValues())
            {
                projectile.Step(Time.fixedDeltaTime);
                if (projectile.InitialTime + projectileSettings.maxLifetime < Time.time)
                {
                    RemoveProjectile(projectile);
                }
            }

            Profiler.EndSample();
            
            if (_projectiles.Count > 0)
            {
                Profiler.BeginSample("ProjectileHandler.FillCommands_1");

                var hitsPool = new NativeArray<RaycastHit>(_projectiles.Count, Allocator.TempJob);
                var commands = new NativeArray<RaycastCommand>(_projectiles.Count, Allocator.TempJob);
                int i = 0;
                foreach (var projectile in _projectiles.GetValues())
                {
                    var vMag = projectile.Velocity.magnitude;
                    commands[i++] = new RaycastCommand(projectile.PreviousPosition, projectile.Velocity / vMag, 
                        new QueryParameters(layerMask: projectileSettings.regularLayerMask, true, QueryTriggerInteraction.Collide, true), vMag * Time.fixedDeltaTime);
                }
                Profiler.EndSample();
                Profiler.BeginSample("ProjectileHandler.ScheduleBatch_1");
                RaycastCommand.ScheduleBatch(commands, hitsPool, 1).Complete();
                Profiler.EndSample();

                Profiler.BeginSample("ProjectileHandler.ClearPreviousHits");
                i = hitsPool.Length - 1;
                _hitsCacheReverse = false;
                HitsCache.Clear();
                _structureHitsMap.Clear();
                Profiler.EndSample();
                Profiler.BeginSample("ProjectileHandler.ExtractHits");
                foreach (var projectile in _projectiles.GetValues())
                {
                    var raycastHit = hitsPool[i];
                    if (raycastHit.collider)
                    {
                        //Debug.DrawRay(commands[i].origin, commands[i].direction * commands[i].distance, Color.red, 5);
                        //Debug.Log($"Collide: {raycastHit.collider.name}");
                        Profiler.BeginSample("ProjectileHandler.TryGetComponent");
                        bool r = raycastHit.collider.TryGetComponent<StructureDamageModelLink>(out var damageModelLink);
                        Profiler.EndSample();
                        if (r)
                        {
                            Profiler.BeginSample("ProjectileHandler.InsertToMap");
                            if (!_structureHitsMap.TryGetValue(damageModelLink, out var mapKey))
                            {
                                _structureHitsMap.Add(damageModelLink, (HitsCache.Count, 1));
                            }
                            else
                            {
                                mapKey.Item2++;
                                _structureHitsMap[damageModelLink] = mapKey;
                            }
                            Profiler.EndSample();
                            Profiler.BeginSample("ProjectileHandler.AddToCache");
                            HitsCache.Add(new StructureRawHit(damageModelLink, projectile, Time.fixedDeltaTime));
                            Profiler.EndSample();
                        }
                        else if (raycastHit.collider.TryGetComponent<IDamagable>(out var damagable))
                        {
                            damagable.Hit(projectile, new HitData(raycastHit.point, raycastHit.normal), ArraySegment<IDamageModifier>.Empty);
                        }

                        projectile.Position = raycastHit.point;
                        RemoveProjectile(projectile);
                    }
                    i--;
                }
                Profiler.EndSample();

                commands.Dispose();
                hitsPool.Dispose();

                _structureHitsCacheOriginal.Clear();
                _structureHitsCacheOriginal.AddRange(HitsCache);

                while (RaycastDamageModels(HitsCache, _structureHitsMap) > 0)
                {
                    var oldHits = HitsCache;
                    _hitsCacheReverse = !_hitsCacheReverse;
                    List<StructureRawHit> aliveHits = HitsCache;
                    aliveHits.Clear();
                    for (var j = 0; j < oldHits.Count; j++)
                    {
                        if(oldHits[j].RemainingTravel.sqrMagnitude > 0.00001f)
                        {
                            aliveHits.Add(oldHits[j]);
                        }
                    }
                }
                RaycastStructures(_structureHitsCacheOriginal, _structureHitsMap);
            }

            OnPostUpdate?.Invoke();
        }

        private void RaycastStructures(List<StructureRawHit> hits, Dictionary<StructureDamageModelLink, (int, int)> map)
        {
            Profiler.BeginSample("ProjectileHandler.AllocateCommands");
            const int hitsPerCommand = 4;
            var hitsPool = new NativeArray<RaycastHit>(hits.Count * hitsPerCommand, Allocator.TempJob);
            var commands = new NativeArray<RaycastCommand>(hits.Count, Allocator.TempJob);
            Profiler.EndSample();
            Profiler.BeginSample("ProjectileHandler.FillCommands");

            Parallel.For(0, hits.Count, FillCommands);
            
            Profiler.EndSample();
            
            void FillCommands(int i)
            {
                //Debug.DrawRay(globalPosForModel, velRelativeToModel, Color.red, 5);
                Vector3 travel = hits[i].ActualPosition + hits[i].RemainingTravel - hits[i].Projectile.PreviousPosition;
                float travelMag = travel.magnitude;
                commands[i] = new RaycastCommand(hits[i].Projectile.PreviousPosition,
                    travel / travelMag,
                    new QueryParameters(layerMask: projectileSettings.structureHitsLayerMask, true,
                        QueryTriggerInteraction.Collide,
                        true), travelMag);
            }
            
            Profiler.BeginSample("ProjectileHandler.ScheduleBatch");
            RaycastCommand.ScheduleBatch(commands, hitsPool, 1, hitsPerCommand).Complete();
            Profiler.EndSample();
            
            for (var i = 0; i < hits.Count; i++)
            {
                for (int j = 0; j < hitsPerCommand; j++)
                {
                    var hit = hitsPool[i * hitsPerCommand + j];
                    if (hit.colliderInstanceID != 0)
                    {
                        if (_damagableMap.TryGetValue(hit.colliderInstanceID, out var damagable))
                        {
                            Vector3 cross = Vector3.Cross(hits[i].ActualPosition - hits[i].Projectile.PreviousPosition, Vector3.up).normalized;
                            Debug.DrawLine(hits[i].Projectile.PreviousPosition, hit.point, Color.red, 5);
                            Debug.DrawRay(hit.point, cross * 0.2f, Color.red, 5);
                            Debug.DrawRay(hit.point, -cross * 0.2f, Color.red, 5);
                            damagable.Hit(hits[i].Projectile, new HitData(hit.point, hit.normal), ArraySegment<IDamageModifier>.Empty);
                        }
                    }
                }

                if (hits[i].Result == StructureHitResult.Stacked)
                {
                    RemoveProjectile(hits[i].Projectile);
                }
            }
        }
        
        private int RaycastDamageModels(List<StructureRawHit> hits, Dictionary<StructureDamageModelLink, (int, int)> map)
        {
            if (hits.Count == 0) return 0;
            
            Profiler.BeginSample("ProjectileHandler.AllocateData");
            NativeArray<int> modelsAddress = new NativeArray<int>(hits.Count, Allocator.TempJob);
            var models = new StructureDamageModel[map.Count];
            var modelsPositions = new NativeArray<Vector3>(map.Count, Allocator.TempJob);
            var sourcesPositions = new NativeArray<Vector3>(map.Count, Allocator.TempJob);
            var sourcesRotations = new NativeArray<Quaternion>(map.Count, Allocator.TempJob);
            Profiler.EndSample();
            Profiler.BeginSample("ProjectileHandler.SetupData");
            {
                int i = 0;
                foreach (KeyValuePair<StructureDamageModelLink, (int index, int count)> kv in map)
                {
                    models[i] = kv.Key.ModelPool.Get();
                    modelsPositions[i] = models[i].Root.position;
                    sourcesPositions[i] = kv.Key.Structure.transform.position;
                    sourcesRotations[i] = kv.Key.Structure.transform.rotation;
                    for (int j = kv.Value.index; j < kv.Value.index + kv.Value.count; j++)
                    {
                        modelsAddress[j] = i;
                    }
                    i++;
                }
            }
            Profiler.EndSample();
            Profiler.BeginSample("ProjectileHandler.AllocateCommands");

            var hitsPool = new NativeArray<RaycastHit>(hits.Count, Allocator.TempJob);
            var commands = new NativeArray<RaycastCommand>(hits.Count, Allocator.TempJob);
            Profiler.EndSample();
            Profiler.BeginSample("ProjectileHandler.FillCommands");

            Parallel.For(0, hits.Count, FillCommands);
            
            Profiler.EndSample();
            
            void FillCommands(int i)
            {
                var invRot = Quaternion.Inverse(sourcesRotations[modelsAddress[i]]);
                Vector3 posRelativeToSource = invRot * (hits[i].ActualPosition - sourcesPositions[modelsAddress[i]]);
                Vector3 globalPosForModel = posRelativeToSource + modelsPositions[modelsAddress[i]];
                Vector3 travelRelativeToModel = invRot * hits[i].RemainingTravel;
                
                //Debug.DrawRay(globalPosForModel, velRelativeToModel, Color.red, 5);
                var travelMag = travelRelativeToModel.magnitude;
                commands[i] = new RaycastCommand(globalPosForModel,
                    travelRelativeToModel / travelMag,
                    new QueryParameters(layerMask: projectileSettings.structureHitsLayerMask, true,
                        QueryTriggerInteraction.Collide,
                        true), travelMag);
            }
            
            
            
            
            Profiler.BeginSample("ProjectileHandler.ScheduleBatch");
            RaycastCommand.ScheduleBatch(commands, hitsPool, 1).Complete();
            Profiler.EndSample();
            
            

            int aliveProjectiles = 0;
            for (int i = 0; i < hitsPool.Length; i++)
            {
                var hit = hits[i];
                if (hitsPool[i].colliderInstanceID == 0 || !ProcessHit(ref hit, 
                        hitsPool[i], 
                        modelsPositions[modelsAddress[i]], 
                        sourcesRotations[modelsAddress[i]],
                        sourcesPositions[modelsAddress[i]],
                        ref aliveProjectiles))
                {
                    hit.StepTo(hit.ActualPosition + hit.RemainingTravel);
                }
                hits[i] = hit;
            }
            
            Profiler.BeginSample("ProjectileHandler.Dispose");
            foreach (var k in map.Keys)
            {
                k.ModelPool.Reset();
            }
            modelsPositions.Dispose();
            sourcesPositions.Dispose();
            sourcesRotations.Dispose();
            modelsAddress.Dispose();
            Profiler.EndSample();
            
            return aliveProjectiles;
        }

        private bool ProcessHit(ref StructureRawHit hit, RaycastHit raycastHit, Vector3 modelsPosition, Quaternion sourcesRotation, Vector3 sourcePosition, ref int aliveProjectiles)
        {
            if (_armorMap.TryGetValue(raycastHit.colliderInstanceID, out var armorData))
            {
                bool stepDone = false;
                switch (hit.Projectile.ShellData.chargeType)
                {
                    case ChargeType.Ap:
                        float velMag = hit.Projectile.Velocity.magnitude;
                        float dot = Vector3.Dot(hit.Projectile.Velocity / velMag, raycastHit.normal);
                        float maxThickness = GetMaxArmorThickness(hit.Projectile.ShellData.mass,
                            hit.Projectile.ShellData.caliber.DiameterDecimeters,
                            velMag, dot, armorData.durability);
                            
                        hit.Projectile.SlowDown(Mathf.Max((maxThickness - armorData.thickness) / maxThickness, 0));
                        Vector3 localPoint = raycastHit.point - modelsPosition;
                        hit.StepTo(sourcesRotation * localPoint + sourcePosition);
                            
                        if (maxThickness < armorData.thickness)
                        {
                            hit.RemainingTravel = Vector3.zero;
                            hit.Result = StructureHitResult.Stacked;
                        }
                        else
                        {
                            hit.Result = StructureHitResult.Penetrated;
                        }

                        stepDone = true;
                        break;
                }
                
                if (hit.RemainingTravel.sqrMagnitude > 0.001f)
                {
                    aliveProjectiles++;
                }
                return stepDone;
            }

            return false;
        }

        private void AddProjectile(ProjectileInstance instance)
        {
            var key = _projectiles.Add(instance);
            instance.InjectKey(key);
        }
        
        private void RemoveProjectile(ProjectileInstance instance)
        {
            OnProjectileRemoved?.Invoke(instance.Id);
            instance.Dispose();
            _projectiles.Remove(instance.Id);
        }
        
        /// <summary>
        /// Returns max thickness of armor in mm can be penetrated by projectile
        /// </summary>
        /// <param name="shellMass">mass, kg</param>
        /// <param name="caliber">caliber, decimeters</param>
        /// <param name="shellSpeed">shell's speed, meters per second</param>
        /// <param name="dot">cosine of angle between shell velocity and normal of armor surface</param>
        /// <param name="armorDurabilityCoefficient"></param>
        /// <returns>max penetration thickness in millimeters</returns>
        public static float GetMaxArmorThickness(float shellMass, float caliber, float shellSpeed, float dot, float armorDurabilityCoefficient) //https://ru.wikipedia.org/wiki/%D0%91%D1%80%D0%BE%D0%BD%D0%B5%D0%BF%D1%80%D0%BE%D0%B1%D0%B8%D0%B2%D0%B0%D0%B5%D0%BC%D0%BE%D1%81%D1%82%D1%8C
        {
            return Mathf.Pow(shellSpeed / armorDurabilityCoefficient, 1.43f) * (Mathf.Pow(shellMass, 0.71f) / Mathf.Pow(caliber, 1.07f)) * Mathf.Pow(dot, 1.4f) * 100;
        }
    }
}