using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Misc
{
    public struct SmKey
    {
        public readonly int Index;
        public readonly int Generation;

        public SmKey(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }
    }

    public interface IReadOnlySlotMap<T> : IEnumerable<KeyValuePair<SmKey, T>>
    {
        public bool IsValid(SmKey smKey);
        public bool TryGet(SmKey smKey, out T value);
        public int Count { get; }
        IEnumerable<T> GetValues();
        IEnumerable<SmKey> GetKeys();
        T this[SmKey smKey] { get; }
    }

    public class SlotMap<T> : IReadOnlySlotMap<T>, IEnumerable<KeyValuePair<SmKey, T>>
    {
        private struct Slot
        {
            public int DenseIndex; // Ссылка на массив Values
            public int Generation; // Текущее поколение
            public int NextFree; // Индекс следующего свободного слота
        }

        private Slot[] _slots; // "Sparse" массив с метаданными
        private int[] _dense; // Обратные индексы (Dense -> Sparse)
        private T[] _values; // Сами данные

        private int _count; // Кол-во живых элементов
        private int _freeHead = -1; // Голова списка свободных мест

        public SlotMap(int initialCapacity = 16)
        {
            _slots = new Slot[initialCapacity];
            _dense = new int[initialCapacity];
            _values = new T[initialCapacity];

            // Инициализируем список свободных мест
            for (int i = 0; i < initialCapacity; i++)
            {
                _slots[i].NextFree = i + 1;
                _slots[i].Generation = 1;
            }

            _slots[initialCapacity - 1].NextFree = -1;
            _freeHead = 0;
        }
        
        public int Count => _count;

        public SmKey Add(T value)
        {
            int index = _freeHead;
            _freeHead = _slots[index].NextFree;
            if (_freeHead == -1)
            {
                Resize(index);
            }
            
            _slots[index].DenseIndex = _count;
            _dense[_count] = index;
            _values[_count] = value;

            _count++;
            return new SmKey(index, _slots[index].Generation);
        }

        public bool Remove(SmKey smKey)
        {
            if (!IsValid(smKey)) return false;

            int slotIndex = smKey.Index;
            int denseIndex = _slots[slotIndex].DenseIndex;

            // Swap back: переносим последний элемент на место удаляемого
            int lastDenseIndex = _count - 1;
            int lastSlotIndex = _dense[lastDenseIndex];

            _values[denseIndex] = _values[lastDenseIndex];
            _dense[denseIndex] = lastSlotIndex;
            _slots[lastSlotIndex].DenseIndex = denseIndex;

            // Освобождаем слот
            _slots[slotIndex].Generation++; // Инвалидируем старые ключи
            _slots[slotIndex].NextFree = _freeHead;
            _freeHead = slotIndex;

            _count--;
            return true;
        }

        public bool IsValid(SmKey smKey) =>
            smKey.Index >= 0 &&
            smKey.Index < _slots.Length &&
            _slots[smKey.Index].Generation == smKey.Generation;

        public ref T GetRef(SmKey smKey) => ref _values[_slots[smKey.Index].DenseIndex];

        public bool TryGet(SmKey smKey, out T value)
        {
            if (IsValid(smKey))
            {
                value = this[smKey];
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        private void Resize(int currentDeadEnd)
        {
            int oldCapacity = _slots.Length;
            int newSize = Mathf.NextPowerOfTwo(oldCapacity + 1);
            Array.Resize(ref _slots, newSize);
            Array.Resize(ref _dense, newSize);
            Array.Resize(ref _values, newSize);
            _freeHead = oldCapacity;
            _slots[currentDeadEnd].NextFree = oldCapacity;
            for (int i = oldCapacity; i < newSize; i++)
            {
                _slots[i].NextFree = i + 1;
                _slots[i].Generation = 1;
            }

            _slots[newSize - 1].NextFree = -1;
        }

        public IEnumerator<KeyValuePair<SmKey, T>> GetEnumerator()
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                var index = _dense[i];
                yield return new KeyValuePair<SmKey, T>(new SmKey(index, _slots[index].Generation), _values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<SmKey> GetKeys()
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                var index = _dense[i];
                yield return new SmKey(index, _slots[index].Generation);
            }
        }

        public T this[SmKey smKey]
        {
            get => _values[_slots[smKey.Index].DenseIndex];
        }

        public IEnumerable<T> GetValues()
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                yield return _values[i];
            }
        }
    }
}