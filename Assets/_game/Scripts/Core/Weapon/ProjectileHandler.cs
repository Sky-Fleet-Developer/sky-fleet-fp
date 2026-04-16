using System;
using System.Collections.Generic;
using Core.Configurations;
using Core.Items;
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
        private List<ProjectileInstance> _projectiles = new(1000);
        private int _projectilesCounter;
        
        //public event Action<int, Vector3, Vector3> OnProjectileWaterInteraction;
        public event Action<int> OnProjectileAdded;
        public event Action<int> OnProjectileRemoved;
        public event Action OnPostUpdate;
        
        public IReadOnlyList<ProjectileInstance> Projectiles => _projectiles;
        
        public void InstallBindings(DiContainer container)
        {
            container.Bind<ProjectileHandler>().FromInstance(this).AsSingle();
        }

        private void FixedUpdate()
        {
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Step(Time.fixedDeltaTime);
                if (_projectiles[i].InitialTime + projectileSettings.maxLifetime < Time.time)
                {
                    RemoveParticle(i);
                }
            }

            if (_projectiles.Count > 0)
            {
                var hitsPool = new NativeArray<RaycastHit>(_projectiles.Count, Allocator.TempJob);
                var commands = new NativeArray<RaycastCommand>(_projectiles.Count, Allocator.TempJob);
                for (int i = 0; i < _projectiles.Count; i++)
                {
                    var vMag = _projectiles[i].Velocity.magnitude;
                    commands[i] = new RaycastCommand(_projectiles[i].PreviousPosition, _projectiles[i].Velocity / vMag, 
                        new QueryParameters(layerMask: projectileSettings.layerMask, true, QueryTriggerInteraction.Collide, true), vMag * Time.fixedDeltaTime);
                }
                var handle = RaycastCommand.ScheduleBatch(commands, hitsPool, 1);
                handle.Complete();

                for (int i = hitsPool.Length - 1; i >= 0; i--)
                {
                    var raycastHit = hitsPool[i];
                    if (raycastHit.collider != null)
                    {
                        //Debug.DrawRay(commands[i].origin, commands[i].direction * commands[i].distance, Color.red, 5);
                        //Debug.Log($"Collide: {raycastHit.collider.name}");
                        if (raycastHit.collider.TryGetComponent<IDamagable>(out var damagable))
                        {
                            damagable.Hit(_projectiles[i], raycastHit.point, raycastHit.normal, ArraySegment<IDamageModifier>.Empty);
                        }
                        _projectiles[i].Position = raycastHit.point;
                        RemoveParticle(i);
                    }
                }
                commands.Dispose();
                hitsPool.Dispose();
            }

            OnPostUpdate?.Invoke();
        }

        private void RemoveParticle(int i)
        {
            OnProjectileRemoved?.Invoke(i);
            _projectiles[i].Dispose();
            //_projectiles.RemoveAt(i); TODO: remove old particles, implement stable indexing
        }

        public void MakeProjectile(IKineticWeapon weapon, ItemInstance shell)
        {
            var weaponData = _itemsTable.GetKineticWeapon(weapon.SourceItem.Sign.Id);
            var shellData = _itemsTable.GetShell(shell.Sign.Id);
            Vector3 spread = Random.insideUnitSphere * (weaponData.spread * 0.01f);
            float speed = weaponData.impulse / shell.Sign.GetSingleMass();
            
            ProjectileInstance instance = new ProjectileInstance(weapon.Muzzle.position, weapon.Muzzle.forward + spread, weapon.Velocity, speed, shellData, _projectilesCounter++);
            AddProjectile(instance);
            OnProjectileAdded?.Invoke(_projectilesCounter - 1);
        }

        private void AddProjectile(ProjectileInstance instance)
        {
            _projectiles.Add(instance);
        }
    }
}