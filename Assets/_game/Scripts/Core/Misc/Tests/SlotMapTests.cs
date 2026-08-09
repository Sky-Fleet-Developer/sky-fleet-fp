using System.Collections.Generic;
using NUnit.Framework;

namespace Core.Misc.Tests
{
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