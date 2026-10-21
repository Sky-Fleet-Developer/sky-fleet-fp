using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Core;
using Core.Explorer;
using Core.Narrative;
using Core.Trading;
using Core.World;
using QnE;
using Runtime.Narrative.Authoring;
using QnE.Unity;
using Sirenix.OdinInspector;
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

        [SerializeField, PropertyTooltip("Every event source of the game (anything that implements IEventDefinitionSource). Sources that are not listed cannot be rebuilt when a save is loaded.")]
        private ScriptableObject[] definitions;
        [SerializeField, PropertyTooltip("Defaults of blackboard properties a save does not have yet.")]
        private BlackboardRegistry registry;
        [SerializeField, MinValue(0.1f)] private float actorSyncPeriod = 1f;
        [SerializeField, PropertyTooltip("Writes events and stages to the console, and enables the Log command.")]
        private bool logProgress = true;

        [Inject(Optional = true)] private IEntityIndex _entityIndex;
        [Inject(Optional = true)] private IWorldSaveSlot _saveSlot;
        [Inject(Optional = true)] private BankSystem _bank;
        private InventoryFacts _inventoryFacts;
        private float _syncTimer;
        private bool _isLoaded;

        public QnEService Service { get; } = new();
        public NarrativeActors Actors { get; private set; }

        private static readonly IReadOnlyDictionary<string, QValue> NoArgs = new Dictionary<string, QValue>();

        private string SavePath => Path.Combine((_saveSlot ??= new DefaultWorldSaveSlot()).GetSlotDirectory(), SaveFileName);

        public void InstallBindings(DiContainer container)
        {
            container.Bind<IQuestsAndEvents>().FromInstance(this);
        }

        public EventInstance Offer(IEventDefinitionSource source, IDictionary<string, QValue> args = null, string entry = null)
        {
            return Service.Offer(source, args, entry);
        }

        public Task Load()
        {
            QnEUnityUtility.AttachLogging(Service);
            if (logProgress) QnEUnityUtility.AttachProgressLog(Service);
            if (_bank != null)
            {
                _inventoryFacts = new InventoryFacts(Service, _bank);
                _inventoryFacts.RegisterCommands();
            }
            if (_entityIndex != null)
            {
                Actors = new NarrativeActors(Service, _entityIndex);
                Actors.RegisterCommands();
            }

            foreach (var asset in definitions ?? Array.Empty<ScriptableObject>())
            {
                if (!asset) continue;
                try
                {
                    if (asset is not IEventDefinitionSource source)
                    {
                        Debug.LogError($"{asset.name} does not implement IEventDefinitionSource", asset);
                        continue;
                    }

                    Service.RegisterSource(source);
                    if (source.IsStandalone) Service.Register(source.Build(source.SourceId, NoArgs));
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

            if (registry) registry.ApplyDefaults(Service.Blackboard);
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
            _syncTimer += Time.deltaTime;
            if (_syncTimer < actorSyncPeriod) return;
            _syncTimer = 0f;
            Actors?.Sync();
            _inventoryFacts?.Sync();
            Service.Pump();
        }

        private void OnDestroy()
        {
            if (QnEDebug.Live == Service) QnEDebug.Live = null;
        }
    }
}
