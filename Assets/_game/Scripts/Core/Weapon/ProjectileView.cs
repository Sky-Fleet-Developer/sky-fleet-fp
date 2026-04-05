using System.Collections.Generic;
using AYellowpaper;
using Core.Misc;
using Core.Utilities;
using UnityEngine;
using Zenject;

namespace Core.Weapon
{
    public class ProjectileView : MonoBehaviour
    {
        //[SerializeField] private InterfaceReference<IVfx, MonoBehaviour> waterSplashVfx;
        [Inject] private ProjectileHandler _projectileHandler;
        [Inject] private RemoteConfigurationHandler _remoteConfigurationHandler;
        private Dictionary<int, IVfx> _vfx = new();
        private ShellTypesSettings _shellTypesSettings;

        private async void Start()
        {
            _projectileHandler.OnProjectileAdded += OnProjectileAdded;
            _projectileHandler.OnProjectileRemoved += OnProjectileRemoved;
            //_projectileHandler.OnProjectileWaterInteraction += WaterSplash;
            _projectileHandler.OnPostUpdate += OnUpdate;
            await _remoteConfigurationHandler.LoadConfigurations();
            _shellTypesSettings = _remoteConfigurationHandler.GetConfig<ShellTypesSettings>();
        }

        private void OnDestroy()
        {
            _projectileHandler.OnProjectileAdded -= OnProjectileAdded;
            _projectileHandler.OnProjectileRemoved -= OnProjectileRemoved;
            _projectileHandler.OnPostUpdate -= OnUpdate;
        }

        private void OnProjectileAdded(int index, int count)
        {
            var instance = _projectileHandler.Projectiles[index];
            var settings = _shellTypesSettings.GetShellTypeSettings(instance.ShellData.chargeType);

            var initialVfxSource = settings.GetInitialVfx(instance.ShellData.caliber.diameter);
            if (initialVfxSource != null)
            {
                var initialVfx = (IVfx)DynamicPool.Instance.Get(initialVfxSource as MonoBehaviour);
                initialVfx.Position = instance.Position;
                initialVfx.Rotation = Quaternion.LookRotation(instance.Velocity);
                initialVfx.Play();
                if (initialVfx.NeedManualReturn)
                {
                    Debug.LogError($"Need manual stop for vfx {initialVfx.transform.name}, but it is not handled");
                }
            }
            var lifetimeVfxSource = settings.GetLifetimeVfx(instance.ShellData.caliber.diameter);

            for (int i = 0; i < count; i++)
            {
                instance = _projectileHandler.Projectiles[index + i];
                if (lifetimeVfxSource != null)
                {
                    var lifetimeVfx = (IVfx)DynamicPool.Instance.Get(lifetimeVfxSource as MonoBehaviour);
                    lifetimeVfx.Position = instance.Position;
                    lifetimeVfx.Rotation = Quaternion.LookRotation(instance.Velocity);
                    _vfx[instance.Id] = lifetimeVfx;
                    lifetimeVfx.Play();
                }
            }
        }
        
        //private void WaterSplash(int index, Vector3 pointOnWater, Vector3 waterNormal)
        //{
        //    var instance = _projectileHandler.Projectiles[index];
        //    var vfx = (IVfx)DynamicPool.Instance.Get(waterSplashVfx.Value as MonoBehaviour);
        //    vfx.Position = pointOnWater;
        //    vfx.Rotation = Quaternion.LookRotation(waterNormal, -instance.Velocity);
        //    vfx.Play();
        //    if (vfx.NeedManualReturn)
        //    {
        //        Debug.LogError($"Need manual stop for vfx {vfx.transform.name}, but it is not handled");
        //    }
        //}
        
        private void OnUpdate()
        {
            foreach (var instance in _projectileHandler.Projectiles)
            {
                if (_vfx.TryGetValue(instance.Id, out var vfx))
                {
                    vfx.Position = instance.Position;
                    vfx.Rotation = Quaternion.LookRotation(instance.Velocity);
                }
            }
        }
        
        private void OnProjectileRemoved(int index)
        {
            var instance = _projectileHandler.Projectiles[index];
            if (_vfx.TryGetValue(instance.Id, out var vfx))
            {
                vfx.Position = instance.Position;
                vfx.Rotation = Quaternion.LookRotation(instance.Velocity);
                vfx.Stop();
            }
        }
    }
}