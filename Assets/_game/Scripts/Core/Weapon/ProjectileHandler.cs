using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
using Core.Misc;
using Unity.Collections;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Core.Weapon
{
    public class ProjectileHandler : MonoBehaviour, IMyInstaller
    {
        [SerializeField] private ProjectileSettings projectileSettings;
        [SerializeField] private float minSpatialLength = 3f;
        [SerializeField] private bool drawQueries;
        [Inject] private ItemsTable _itemsTable;
        private SlotMap<ProjectileInstance> _projectiles = new(512);
        
        //public event Action<int, Vector3, Vector3> OnProjectileWaterInteraction;
        public event Action<ProjectileInstance> OnProjectileAdded;
        public event Action<SmKey> OnProjectileRemoved;
        public event Action OnPostUpdate;
        
        public IReadOnlySlotMap<ProjectileInstance> Projectiles => _projectiles;
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<ProjectileHandler>().FromInstance(this).AsSingle();
        }

        private void FixedUpdate()
        {
            foreach (var projectile in _projectiles.GetValues())
            {
                projectile.Step(Time.fixedDeltaTime);
                if (projectile.InitialTime + projectileSettings.maxLifetime < Time.time)
                {
                    RemoveParticle(projectile);
                }
            }

            if (_projectiles.Count > 0)
            {
                var hitsPool = new NativeArray<RaycastHit>(_projectiles.Count, Allocator.TempJob);
                var commands = new NativeArray<RaycastCommand>(_projectiles.Count, Allocator.TempJob);
                int i = 0;
                foreach (var projectile in _projectiles.GetValues())
                {
                    var vMag = projectile.Velocity.magnitude;
                    commands[i++] = new RaycastCommand(projectile.PreviousPosition, projectile.Velocity / vMag, 
                        new QueryParameters(layerMask: projectileSettings.layerMask, true, QueryTriggerInteraction.Collide, true), vMag * Time.fixedDeltaTime);
                }
                
                var handle = RaycastCommand.ScheduleBatch(commands, hitsPool, 1);
                handle.Complete();
                
                i = hitsPool.Length - 1;
                foreach (var projectile in _projectiles.GetValues())
                {
                    var raycastHit = hitsPool[i];
                    if (raycastHit.collider != null)
                    {
                        //Debug.DrawRay(commands[i].origin, commands[i].direction * commands[i].distance, Color.red, 5);
                        //Debug.Log($"Collide: {raycastHit.collider.name}");
                        if (raycastHit.collider.TryGetComponent<IDamagable>(out var damagable))
                        {
                            damagable.Hit(projectile, raycastHit.point, raycastHit.normal, ArraySegment<IDamageModifier>.Empty);
                        }
                        projectile.Position = raycastHit.point;
                        RemoveParticle(projectile);
                    }
                    i--;
                }

                commands.Dispose();
                hitsPool.Dispose();
            }

            OnPostUpdate?.Invoke();
        }

        private void RemoveParticle(ProjectileInstance instance)
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