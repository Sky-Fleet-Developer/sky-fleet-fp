using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Core.Structure.Damage;
using Core.Utilities;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using Zenject;
using Random = UnityEngine.Random;

namespace Core.Weapon
{
    [BurstCompile]
    public class ProjectileHandler : MonoBehaviour, IMyInstaller
    {
        [SerializeField] private ProjectileSettings projectileSettings;
        [SerializeField] private float minSpatialLength = 3f;
        [SerializeField] private bool drawQueries;
        [Inject] private ItemsTable _itemsTable;
        [Inject] private StructureDamageProfileHub _structureDamageProfileHub;
        private SlotMap<ProjectileInstance> _projectiles = new(512);
        private List<StructureRawHit> structureHitsCache = new(32);
        private Dictionary<StructureDamageModelLink, (int, int)> structureHitsMap = new(); // StructureDamageModelLink, (startIndex, count)
        //public event Action<int, Vector3, Vector3> OnProjectileWaterInteraction;
        public event Action<ProjectileInstance> OnProjectileAdded;
        public event Action<SmKey> OnProjectileRemoved;
        public event Action OnPostUpdate;

        private struct StructureRawHit
        {
            public StructureDamageModelLink ModelLink;
            public ProjectileInstance Projectile;

            public StructureRawHit(StructureDamageModelLink damageModelLink, ProjectileInstance projectile)
            {
                ModelLink = damageModelLink;
                Projectile = projectile;
            }
        }
        
        public IReadOnlySlotMap<ProjectileInstance> Projectiles => _projectiles;
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<ProjectileHandler>().FromInstance(this).AsSingle();
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
                structureHitsCache.Clear();
                structureHitsMap.Clear();
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
                            if (!structureHitsMap.TryGetValue(damageModelLink, out var mapKey))
                            {
                                structureHitsMap.Add(damageModelLink, (structureHitsCache.Count, 1));
                            }
                            else
                            {
                                mapKey.Item2++;
                                structureHitsMap[damageModelLink] = mapKey;
                            }
                            Profiler.EndSample();
                            Profiler.BeginSample("ProjectileHandler.AddToCache");
                            structureHitsCache.Add(new StructureRawHit(damageModelLink, projectile));
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

                RaycastStructures(structureHitsCache, structureHitsMap);
            }

            OnPostUpdate?.Invoke();
        }
        
        private void RaycastStructures(List<StructureRawHit> hits, Dictionary<StructureDamageModelLink, (int, int)> map)
        {
            if (hits.Count == 0) return;
            
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
            float fixedDeltaTime = Time.fixedDeltaTime;
            Profiler.EndSample();
            Profiler.BeginSample("ProjectileHandler.FillCommands");

            Parallel.For(0, hits.Count, FillCommands);
            
            Profiler.EndSample();

            void FillCommands(int i)
            {
                var invRot = Quaternion.Inverse(sourcesRotations[modelsAddress[i]]);
                Vector3 posRelativeToSource = invRot * (hits[i].Projectile.PreviousPosition - sourcesPositions[modelsAddress[i]]);
                Vector3 globalPosForModel = posRelativeToSource + modelsPositions[modelsAddress[i]];
                Vector3 velRelativeToModel = invRot * hits[i].Projectile.Velocity;
                
                //Debug.DrawRay(globalPosForModel, velRelativeToModel, Color.red, 5);
                var vMag = velRelativeToModel.magnitude;
                commands[i] = new RaycastCommand(globalPosForModel,
                    velRelativeToModel / vMag,
                    new QueryParameters(layerMask: projectileSettings.structureHitsLayerMask, true,
                        QueryTriggerInteraction.Collide,
                        true), vMag * fixedDeltaTime);
            }
            Profiler.BeginSample("ProjectileHandler.ScheduleBatch");
            RaycastCommand.ScheduleBatch(commands, hitsPool, 1).Complete();
            Profiler.EndSample();

            for (int i = 0; i < hitsPool.Length; i++)
            {
                if (hitsPool[i].collider)
                {
                    if (hitsPool[i].collider.TryGetComponent<Armor>(out var armor))
                    {
                        OnProjectileHitArmor(hits[i].Projectile, armor);
                    }
                }
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
        }

        private int c = 0;
        private void OnProjectileHitArmor(ProjectileInstance instance, Armor armor)
        {
            c++;
        }

        private void RemoveProjectile(ProjectileInstance instance)
        {
            OnProjectileRemoved?.Invoke(instance.Id);
            instance.Dispose();
            _projectiles.Remove(instance.Id);
        }

        public void MakeProjectile(IKineticWeapon weapon, ItemInstance shell)
        {
            var weaponData = _itemsTable.GetKineticWeapon(weapon.SourceItem.Sign.Id);
            var shellData = _itemsTable.GetShell(shell.Sign.Id);
            Vector3 spread = Random.insideUnitSphere * (weaponData.spread * 0.01f);
            float speed = weaponData.impulse / shell.Sign.GetSingleMass();
            
            ProjectileInstance instance = new ProjectileInstance(weapon.Muzzle.position, weapon.Muzzle.forward + spread, weapon.Velocity, speed, shellData);
            AddProjectile(instance);
            OnProjectileAdded?.Invoke(instance);
        }

        private void AddProjectile(ProjectileInstance instance)
        {
            var key = _projectiles.Add(instance);
            instance.InjectKey(key);
        }
    }
}