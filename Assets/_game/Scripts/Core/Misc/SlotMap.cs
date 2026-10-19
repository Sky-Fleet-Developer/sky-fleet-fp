using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Core.ContentSerializer;
using UnityEngine;

namespace Core.Misc
{
    public readonly struct SmKey : IEquatable<SmKey>
    {
        public readonly int Index;
        public readonly int Generation;

        public SmKey(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }

        public bool Equals(SmKey other) => Index == other.Index && Generation == other.Generation;
        public override bool Equals(object obj) => obj is SmKey other && Equals(other);
        public override int GetHashCode() => (Index * 397) ^ Generation;
        public static bool operator ==(SmKey a, SmKey b) => a.Equals(b);
        public static bool operator !=(SmKey a, SmKey b) => !a.Equals(b);
        public override string ToString() => $"{Index}:{Generation}";
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

        /// <summary>
        /// Writes the full state, including generations and the free list, so the keys issued earlier stay valid after <see cref="Read"/>.
        /// </summary>
        public void Write(Stream stream, Action<Stream, T> writeValue)
        {
            stream.WriteInt(_slots.Length);
            stream.WriteInt(_count);
            stream.WriteInt(_freeHead);
            for (int i = 0; i < _slots.Length; i++)
            {
                stream.WriteInt(_slots[i].DenseIndex);
                stream.WriteInt(_slots[i].Generation);
                stream.WriteInt(_slots[i].NextFree);
            }

            for (int i = 0; i < _count; i++)
            {
                stream.WriteInt(_dense[i]);
                writeValue(stream, _values[i]);
            }
        }

        public static SlotMap<T> Read(Stream stream, Func<Stream, T> readValue)
        {
            int capacity = stream.ReadInt();
            var map = new SlotMap<T>(Mathf.Max(1, capacity));
            map._count = stream.ReadInt();
            map._freeHead = stream.ReadInt();
            for (int i = 0; i < capacity; i++)
            {
                map._slots[i].DenseIndex = stream.ReadInt();
                map._slots[i].Generation = stream.ReadInt();
                map._slots[i].NextFree = stream.ReadInt();
            }

            for (int i = 0; i < map._count; i++)
            {
                map._dense[i] = stream.ReadInt();
                map._values[i] = readValue(stream);
            }

            return map;
        }

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