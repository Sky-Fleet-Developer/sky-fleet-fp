using System;
using System.IO;
using System.Threading.Tasks;
using Core;
using Core.Explorer;
using Core.Narrative;
using Core.World;
using QnE;
using QnE.Unity;
using UnityEngine;
using Zenject;

namespace Runtime.Narrative
{
    /// <summary>
    /// Owns the <see cref="QnEService"/>: registers the authored events, ticks them, keeps the actor facts up to date
    /// and restores or saves the narrative state. Place it on a root scene object, like the other installers.
    /// </summary>
    public class QuestsAndEventsService : MonoBehaviour, IMyInstaller, ILoadAtStart, IQuestsAndEvents
    {
        private const string SaveFileName = "narrative.json";

        [SerializeField] private EventDefinitionAsset[] definitions;
        [SerializeField, Min(0.1f)] private float actorSyncPeriod = 1f;

        [Inject(Optional = true)] private IEntityIndex _entityIndex;
        [Inject(Optional = true)] private IWorldSaveSlot _saveSlot;
        private float _syncTimer;
        private bool _isLoaded;

        public QnEService Service { get; } = new();
        public NarrativeActors Actors { get; private set; }

        private string SavePath => Path.Combine((_saveSlot ??= new DefaultWorldSaveSlot()).GetSlotDirectory(), SaveFileName);

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IQuestsAndEvents>().FromInstance(this);
        }

        public Task Load()
        {
            QnEUnityUtility.AttachLogging(Service);
            if (_entityIndex != null)
            {
                Actors = new NarrativeActors(Service, _entityIndex);
                Actors.RegisterCommands();
            }

            foreach (var asset in definitions ?? Array.Empty<EventDefinitionAsset>())
            {
                if (!asset) continue;
                try
                {
                    Service.Register(asset.Build());
                }
                catch (Exception e)
                {
                    Debug.LogException(e, asset);
                }
            }

            if (File.Exists(SavePath))
            {
                try
                {
                    Service.Restore(QnEUnityUtility.FromJson(File.ReadAllText(SavePath)));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            QnEDebug.Live = Service;
            _isLoaded = true;
            return Task.CompletedTask;
        }

        public void Save()
        {
            string path = SavePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, QnEUnityUtility.ToJson(Service.Save()));
            File.Copy(temp, path, true);
            File.Delete(temp);
        }

        private void Update()
        {
            if (!_isLoaded) return;
            Service.Tick(Time.deltaTime);
            if (Actors == null) return;
            _syncTimer += Time.deltaTime;
            if (_syncTimer < actorSyncPeriod) return;
            _syncTimer = 0f;
            Actors.Sync();
            Service.Pump();
        }

        private void OnDestroy()
        {
            if (QnEDebug.Live == Service) QnEDebug.Live = null;
        }
    }
}
