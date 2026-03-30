using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core.Misc
{
    /// <summary>
    /// Tick() will be called every TickRate frame
    /// </summary>
    public interface ITickable
    {
        int TickRate { get; }
        void Tick();
    }

    public class TickService : MonoBehaviour
    {
        // --- Статические правила ---
        private static readonly Dictionary<Type, HashSet<Type>> RunBeforeRules = new Dictionary<Type, HashSet<Type>>();
        private static readonly Dictionary<Type, bool> IsFixedUpdateType = new Dictionary<Type, bool>();
        private static int _globalRulesVersion = 0;

        // --- Локальные списки менеджера ---
        private readonly List<ITickable> _updateTickables = new List<ITickable>();
        private readonly List<ITickable> _fixedTickables = new List<ITickable>();

        // Очереди изменений (общие для обоих циклов)
        private readonly List<ITickable> _pendingAdds = new List<ITickable>();
        private readonly List<ITickable> _pendingRemoves = new List<ITickable>();

        private int _localRulesVersion = -1;
        private uint _updateFrames = 0;
        private uint _fixedFrames = 0;

        public static void SetOrderAfter(Type type, params Type[] types)
        {
            foreach (var t in types) AddRule(t, type);
            _globalRulesVersion++;
        }

        public static void SetOrderBefore(Type type, params Type[] types)
        {
            foreach (var t in types) AddRule(type, t);
            _globalRulesVersion++;
        }

        /// <summary>
        /// Назначает, должен ли конкретный тип обновляться в FixedUpdate (true) или в Update (false).
        /// По умолчанию все обновляются в Update.
        /// </summary>
        public static void SetUpdate(Type type, bool fixedUpdate)
        {
            IsFixedUpdateType[type] = fixedUpdate;
            _globalRulesVersion++; // Триггерит пересортировку и перераспределение по спискам
        }

        public void Add(ITickable tickable)
        {
            if (tickable == null) return;

            if (!_updateTickables.Contains(tickable) &&
                !_fixedTickables.Contains(tickable) &&
                !_pendingAdds.Contains(tickable))
            {
                _pendingAdds.Add(tickable);
            }

            _pendingRemoves.Remove(tickable);
        }

        public void Remove(ITickable tickable)
        {
            if (tickable == null) return;

            if (!_pendingRemoves.Contains(tickable))
            {
                _pendingRemoves.Add(tickable);
            }

            _pendingAdds.Remove(tickable);
        }

        private void Update()
        {
            ProcessPendingAndSort();
            _updateFrames++;

            for (int i = 0; i < _updateTickables.Count; i++)
            {
                var t = _updateTickables[i];
                if (t.TickRate > 0 && _updateFrames % t.TickRate == 0) t.Tick();
            }
        }

        private void FixedUpdate()
        {
            ProcessPendingAndSort();
            _fixedFrames++;

            for (int i = 0; i < _fixedTickables.Count; i++)
            {
                var t = _fixedTickables[i];
                if (t.TickRate > 0 && _fixedFrames % t.TickRate == 0) t.Tick();
            }
        }

        /// <summary>
        /// Применяет отложенные добавления/удаления и сортирует списки.
        /// Вызывается в начале Update и FixedUpdate (в зависимости от того, что произойдет раньше).
        /// </summary>
        private void ProcessPendingAndSort()
        {
            bool needsSort = false;

            // 1. Удаления (удаление не ломает порядок, пересортировка не нужна)
            if (_pendingRemoves.Count > 0)
            {
                foreach (var t in _pendingRemoves)
                {
                    _updateTickables.Remove(t);
                    _fixedTickables.Remove(t);
                }

                _pendingRemoves.Clear();
            }

            // 2. Добавления
            if (_pendingAdds.Count > 0)
            {
                foreach (var t in _pendingAdds)
                {
                    PlaceInCorrectList(t);
                }

                _pendingAdds.Clear();
                needsSort = true;
            }

            // 3. Если статические правила изменились в рантайме, перераспределяем всё заново
            if (_localRulesVersion != _globalRulesVersion)
            {
                var allTickables = _updateTickables.Concat(_fixedTickables).ToList();
                _updateTickables.Clear();
                _fixedTickables.Clear();

                foreach (var t in allTickables)
                {
                    PlaceInCorrectList(t);
                }

                needsSort = true;
                _localRulesVersion = _globalRulesVersion;
            }

            // 4. Пересортировка при необходимости
            if (needsSort)
            {
                SortTickables(_updateTickables);
                SortTickables(_fixedTickables);
            }
        }

        private void PlaceInCorrectList(ITickable t)
        {
            bool isFixed = IsFixedUpdateType.TryGetValue(t.GetType(), out var fixedVal) && fixedVal;
            if (isFixed) _fixedTickables.Add(t);
            else _updateTickables.Add(t);
        }

        /// <summary>
        /// Топологическая сортировка для конкретного списка
        /// </summary>
        private void SortTickables(List<ITickable> listToSort)
        {
            if (listToSort.Count == 0) return;

            var groups = new Dictionary<Type, List<ITickable>>();
            foreach (var t in listToSort)
            {
                var type = t.GetType();
                if (!groups.ContainsKey(type)) groups[type] = new List<ITickable>();
                groups[type].Add(t);
            }

            var presentTypes = groups.Keys.ToList();
            var inDegree = presentTypes.ToDictionary(t => t, t => 0);
            var adjList = presentTypes.ToDictionary(t => t, t => new List<Type>());

            foreach (var type in presentTypes)
            {
                if (RunBeforeRules.TryGetValue(type, out var afterTypes))
                {
                    foreach (var after in afterTypes)
                    {
                        if (presentTypes.Contains(after))
                        {
                            adjList[type].Add(after);
                            inDegree[after]++;
                        }
                    }
                }
            }

            var queue = new Queue<Type>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
            var sortedTypes = new List<Type>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                sortedTypes.Add(current);

                foreach (var neighbor in adjList[current])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0) queue.Enqueue(neighbor);
                }
            }

            if (sortedTypes.Count != presentTypes.Count)
            {
                Debug.LogError($"[Tick] Внимание: Обнаружена циклическая зависимость. Сортировка будет частичной.");
                sortedTypes.AddRange(presentTypes.Except(sortedTypes));
            }

            listToSort.Clear();
            foreach (var type in sortedTypes)
            {
                listToSort.AddRange(groups[type]);
            }
        }

        private static void AddRule(Type before, Type after)
        {
            if (!RunBeforeRules.TryGetValue(before, out var afterSet))
            {
                afterSet = new HashSet<Type>();
                RunBeforeRules[before] = afterSet;
            }

            afterSet.Add(after);
        }
    }
}