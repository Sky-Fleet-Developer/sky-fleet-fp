using System;
using System.Collections.Generic;
using QnE;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Runtime.Narrative.Authoring
{
    /// <summary>
    /// The list of blackboard properties the game knows: name, type, default and a note about who uses it.
    /// Authoring tools offer these in dropdowns instead of free text, which prevents typos and wrong value types.
    /// At runtime the defaults fill the keys a save does not hold yet.
    /// </summary>
    [CreateAssetMenu(menuName = "SF/Narrative/Blackboard Registry", fileName = "BlackboardRegistry")]
    public class BlackboardRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [PropertyTooltip("Blackboard key. Global keys are saved with the game. Do not rename once the game is released.")]
            public string name;
            public ValueKind kind;
            [PropertyTooltip("Shown as a folder in dropdowns, for example 'player' or 'quest.wizard'.")]
            public string group;
            [PropertyTooltip("Who publishes or changes it, what it means.")]
            [MultiLineProperty(2)] public string description;
            [PropertyTooltip("Set when a game has no value for the key. Ignored for scoped keys ('@.', '$.').")]
            public bool applyDefault = true;
            [ShowIf(nameof(applyDefault))] public ValueAsset defaultValue = new();
        }

        [SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => entries;

        public bool TryGet(string name, out Entry entry)
        {
            foreach (var candidate in entries)
            {
                if (candidate.name != name) continue;
                entry = candidate;
                return true;
            }

            entry = null;
            return false;
        }

        /// <summary>Sets the default of every global key the blackboard does not have yet.</summary>
        public void ApplyDefaults(Blackboard blackboard)
        {
            foreach (var entry in entries)
            {
                if (!entry.applyDefault || string.IsNullOrEmpty(entry.name)) continue;
                if (entry.name.StartsWith("@.", StringComparison.Ordinal) || entry.name.StartsWith("$.", StringComparison.Ordinal)) continue;
                if (blackboard.Has(entry.name)) continue;
                blackboard.SetSilently(entry.name, entry.defaultValue.Build());
            }
        }

        private void OnValidate()
        {
            var names = new HashSet<string>();
            foreach (var entry in entries)
            {
                entry.defaultValue ??= new ValueAsset();
                entry.defaultValue.SetKind(entry.kind);
                if (!string.IsNullOrEmpty(entry.name) && !names.Add(entry.name))
                {
                    Debug.LogWarning($"Blackboard registry: '{entry.name}' is listed twice", this);
                }
            }
        }

#if UNITY_EDITOR
        private static BlackboardRegistry _editorInstance;

        /// <summary>The registry authoring tools use. The first one found in the project.</summary>
        public static BlackboardRegistry EditorInstance
        {
            get
            {
                if (_editorInstance) return _editorInstance;
                var guids = AssetDatabase.FindAssets("t:" + nameof(BlackboardRegistry));
                if (guids.Length == 0) return null;
                if (guids.Length > 1) Debug.LogWarning("There are several blackboard registries; the first one is used");
                return _editorInstance = AssetDatabase.LoadAssetAtPath<BlackboardRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        public void AddEntry(string name, ValueKind kind, ValueAsset defaultValue = null)
        {
            Undo.RecordObject(this, "Add blackboard property");
            var entry = new Entry { name = name, kind = kind };
            if (defaultValue != null)
            {
                entry.defaultValue = defaultValue;
            }

            entry.defaultValue.SetKind(kind);
            entries.Add(entry);
            EditorUtility.SetDirty(this);
        }
#endif
    }

    /// <summary>
    /// A reference to a blackboard property in an authored condition or action: a key picked from the registry (or typed)
    /// and, if wanted, a value of the type that the registry declares for the key.
    /// </summary>
    [Serializable]
    public class BlackboardRef
    {
        [SerializeField, HideInInspector] private string key;
        [SerializeField, HideInInspector] private bool hasValue;
        [ShowIf(nameof(ShowValue))] public ValueAsset value = new();

        public string Key => key;

        public BlackboardRef() { }

        public BlackboardRef(bool hasValue)
        {
            this.hasValue = hasValue;
        }

        private bool ShowValue => hasValue;

#if UNITY_EDITOR
        [ShowInInspector, PropertyOrder(-1), LabelText("Key"), ValueDropdown(nameof(Options), AppendNextDrawer = true)]
        private string KeyEditor
        {
            get => key;
            set
            {
                key = value?.Trim();
                var registry = BlackboardRegistry.EditorInstance;
                if (registry != null && registry.TryGet(key, out var entry))
                {
                    this.value.SetKind(entry.kind);
                    this.value.kindLocked = true;
                }
                else
                {
                    this.value.kindLocked = false;
                }
            }
        }

        private bool IsUnknown
        {
            get
            {
                var registry = BlackboardRegistry.EditorInstance;
                return registry != null && !string.IsNullOrEmpty(key) && !registry.TryGet(key, out _);
            }
        }

        private IEnumerable<ValueDropdownItem<string>> Options()
        {
            var registry = BlackboardRegistry.EditorInstance;
            if (registry == null) yield break;
            foreach (var entry in registry.Entries)
            {
                if (string.IsNullOrEmpty(entry.name)) continue;
                string path = string.IsNullOrEmpty(entry.group) ? entry.name : $"{entry.group}/{entry.name}";
                yield return new ValueDropdownItem<string>($"{path}  [{entry.kind}]", entry.name);
            }
        }

        [InfoBox("The key is not in the blackboard registry.", InfoMessageType.Warning, nameof(IsUnknown))]
        [Button("Add to registry"), ShowIf(nameof(IsUnknown))]
        private void AddToRegistry()
        {
            BlackboardRegistry.EditorInstance.AddEntry(key, hasValue ? value.kind : ValueKind.Bool, hasValue ? value : null);
            value.kindLocked = true;
        }
#endif
    }
}
