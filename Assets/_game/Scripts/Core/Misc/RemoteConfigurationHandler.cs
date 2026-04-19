using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Weapon;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Misc
{
    public class RemoteConfigurationHandler : IDisposable
    {
        private List<AsyncOperationHandle> _configs = new ();
        private Task _loadTask;
        public Task LoadConfigurations()
        {
            if (_loadTask != null) return _loadTask;
            _loadTask = LoadConfigurationsAsync();
            return _loadTask;
        }
        
        private async Task LoadConfigurationsAsync()
        {
            _configs.Add(Addressables.LoadAssetAsync<ShellTypesSettings>(nameof(ShellTypesSettings)));
            foreach (var asyncOperationHandle in _configs)
            {
                await asyncOperationHandle;
            }
        }
        
        public T GetConfig<T>()
        {
            return (T)_configs.Find(x => x.Result is T).Result;
        }

        public void Dispose()
        {
            if (_configs == null)
            {
                return;
            }
            foreach (var asyncOperationHandle in _configs)
            {
                Addressables.Release(asyncOperationHandle);
            }

            _configs = null;
        }

        ~RemoteConfigurationHandler()
        {
            if (_configs != null)
            {
                Dispose();
            }
        }
    }
}