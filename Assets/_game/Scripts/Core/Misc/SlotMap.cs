using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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

    [TestFixture]
    public class SlotMapTests
    {
        private SlotMap<string> _slotMap;

        [SetUp]
        public void Setup()
        {
            // Начинаем с небольшой емкости, чтобы быстрее проверить расширение
            _slotMap = new SlotMap<string>(2);
        }

        [Test]
        public void Add_ShouldReturnValidEntity_AndStoreValue()
        {
            var entity = _slotMap.Add("First");

            Assert.That(_slotMap.IsValid(entity), Is.True);
            Assert.That(_slotMap[entity], Is.EqualTo("First"));
        }

        [Test]
        public void Remove_ShouldMakeEntityInvalid()
        {
            var entity = _slotMap.Add("To be removed");

            bool removed = _slotMap.Remove(entity);

            Assert.That(removed, Is.True);
            Assert.That(_slotMap.IsValid(entity), Is.False);
        }

        [Test]
        public void Add_AfterRemove_ShouldReuseIndexWithNewGeneration()
        {
            var entity1 = _slotMap.Add("First");
            _slotMap.Remove(entity1);

            // Внутренне SlotMap должен переиспользовать индекс entity1
            var entity2 = _slotMap.Add("Second");

            Assert.That(entity2.Index, Is.EqualTo(entity1.Index), "Index should be reused");
            Assert.That(entity2.Generation, Is.GreaterThan(entity1.Generation), "Generation must increase");
            Assert.That(_slotMap.IsValid(entity1), Is.False, "Old entity should remain invalid");
            Assert.That(_slotMap[entity2], Is.EqualTo("Second"));
        }

        [Test]
        public void Remove_MiddleElement_ShouldMaintainIntegrity()
        {
            // Тест логики "Swap Back": удаление из середины плотного массива
            var e1 = _slotMap.Add("Item 1");
            var e2 = _slotMap.Add("Item 2");
            var e3 = _slotMap.Add("Item 3");

            _slotMap.Remove(e2); // Удаляем средний

            Assert.That(_slotMap.IsValid(e1), Is.True);
            Assert.That(_slotMap.IsValid(e3), Is.True);
            Assert.That(_slotMap[e1], Is.EqualTo("Item 1"));
            Assert.That(_slotMap[e3], Is.EqualTo("Item 3"));
        }

        [Test]
        public void Get_ByRef_ShouldAllowUpdatingValue()
        {
            var entity = _slotMap.Add("Initial");

            // Так как Get возвращает ref T, мы можем менять значение напрямую
            ref string valueRef = ref _slotMap.GetRef(entity);
            valueRef = "Updated";

            Assert.That(_slotMap[entity], Is.EqualTo("Updated"));
        }

        [Test]
        public void Add_MoreThanCapacity_ShouldResizeCorrectly()
        {
            List<string> values = new List<string>(20);
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 20; i++)
                {
                    values.Add(i.ToString());
                    _slotMap.Add(i.ToString());
                }
            });
            values.Reverse();
            Assert.That(_slotMap.GetValues(), Is.EquivalentTo(values));
        }

        [Test]
        public void Remove_WithStaleGeneration_ShouldReturnFalse()
        {
            var entity = _slotMap.Add("Value");
            _slotMap.Remove(entity);

            // Создаем фейковую сущность с тем же индексом, но старым поколением
            var staleEntity = new SmKey(entity.Index, entity.Generation);

            // Повторное удаление уже невалидной сущности
            bool result = _slotMap.Remove(staleEntity);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Enumerator_ShouldEnumerateAllValidValues()
        {
            _slotMap.Add("A");
            var entityB = _slotMap.Add("B");
            _slotMap.Add("C");
            
            _slotMap.Remove(entityB);
            
            Assert.That(_slotMap.GetValues(), Is.EquivalentTo(new[] { "C", "A" }));
        }

        [Test]
        public void Enumerator_CanRemoveFirstWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "A")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "B", "C" }));
        }
        
        [Test]
        public void Enumerator_CanRemoveMiddleWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "B")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "A", "C" }));
        }
        
        [Test]
        public void Enumerator_CanRemoveLastWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "C")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "A", "B" }));
        }

        [Test]
        public void Enumerator_CanRemoveTwoFirstWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "B" || kv.Value == "A")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "C" }));
        }
        
        [Test]
        public void Enumerator_CanRemoveTwoLastWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "B" || kv.Value == "C")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "A" }));
        }

        [Test]
        public void Enumerator_CanRemoveLastAndFirstWhenEnumerating()
        {
            _slotMap.Add("A");
            _slotMap.Add("B");
            _slotMap.Add("C");
            List<string> result = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                foreach (var kv in _slotMap)
                {
                    if (kv.Value == "A" || kv.Value == "C")
                    {
                        _slotMap.Remove(kv.Key);
                    }
                    else
                    {
                        result.Add(kv.Value);
                    }
                }
            });

            Assert.That(result, Is.EquivalentTo(new[] { "B" }));
        }
        
        [Test]
        public void Enumerator_ShouldEnumerateKeys()
        {
            var a = _slotMap.Add("A");
            var b = _slotMap.Add("B");
            var c = _slotMap.Add("C");
            _slotMap.Remove(b);
            
            Assert.That(_slotMap.GetKeys(), Is.EquivalentTo(new[] { a, c }));
        }
        
        [Test]
        public void AddAfterRemove()
        {
            var a = _slotMap.Add("A");
            var b = _slotMap.Add("B");
            var c = _slotMap.Add("C");
            _slotMap.Remove(b);
            _slotMap.Remove(c);
            _slotMap.Remove(a);
            
            _slotMap.Add("D");
            _slotMap.Add("E");
            Assert.That(_slotMap.GetValues(), Is.EquivalentTo(new[] { "E", "D" }));
        }
    }
}