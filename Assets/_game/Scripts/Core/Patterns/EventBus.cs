using System;
using System.Collections.Generic;

namespace Core.Patterns
{
    /// <summary>
    /// Простая шина событий для разрыва жёстких связей между UI-экранами.
    /// Ничего не наследует — создаётся и инжектится вручную (new EventBus() / DI-контейнер).
    ///
    /// Событие идентифицируется парой (string key, Type delegateType).
    /// Поскольку Action&lt;T1,T2&gt; и Action&lt;T2,T1&gt; — это разные закрытые generic-типы,
    /// они автоматически считаются разными событиями, даже с одинаковым ключом.
    ///
    /// Пример:
    ///   eventBus.Subscribe("ScreenChanged", OnScreenChanged); // Action<ScreenId>
    ///   eventBus.Publish("ScreenChanged", ScreenId.MainMenu);
    ///   eventBus.Unsubscribe("ScreenChanged", OnScreenChanged);
    /// </summary>
    public sealed class EventBus
    {
        // Внешний ключ - строка (имя события), внутренний - конкретный тип делегата.
        private readonly Dictionary<string, Dictionary<Type, Delegate>> _stringKeyedEvents =
            new Dictionary<string, Dictionary<Type, Delegate>>();

        private readonly Dictionary<Type, Delegate> _typeKeyedEvents =
            new();

        // ---------------------------------------------------------------------
        // Subscribe
        // ---------------------------------------------------------------------
        public void Subscribe<T>(Action<T> handler)
            => AddTypeKeyedHandler(typeof(T), handler);

        public void Subscribe(string key, Action handler)
            => AddStringKeyedHandler(key, handler);

        public void Subscribe<T1>(string key, Action<T1> handler)
            => AddStringKeyedHandler(key, handler);

        public void Subscribe<T1, T2>(string key, Action<T1, T2> handler)
            => AddStringKeyedHandler(key, handler);

        public void Subscribe<T1, T2, T3>(string key, Action<T1, T2, T3> handler)
            => AddStringKeyedHandler(key, handler);

        public void Subscribe<T1, T2, T3, T4>(string key, Action<T1, T2, T3, T4> handler)
            => AddStringKeyedHandler(key, handler);

        // ---------------------------------------------------------------------
        // Unsubscribe
        // ---------------------------------------------------------------------

        public void Unsubscribe<T>(Action<T> handler)
            => RemoveTypeKeyedHandler(typeof(T), handler);

        public void Unsubscribe(string key, Action handler)
            => RemoveStringTypedHandler(key, handler);

        public void Unsubscribe<T1>(string key, Action<T1> handler)
            => RemoveStringTypedHandler(key, handler);

        public void Unsubscribe<T1, T2>(string key, Action<T1, T2> handler)
            => RemoveStringTypedHandler(key, handler);

        public void Unsubscribe<T1, T2, T3>(string key, Action<T1, T2, T3> handler)
            => RemoveStringTypedHandler(key, handler);

        public void Unsubscribe<T1, T2, T3, T4>(string key, Action<T1, T2, T3, T4> handler)
            => RemoveStringTypedHandler(key, handler);

        // ---------------------------------------------------------------------
        // Publish
        // ---------------------------------------------------------------------

        public void Publish<T>(T arg)
        {
            if (TryGetTypeKeyedDelegate<Action<T>>(typeof(T), out var handler))
            {
                handler.Invoke(arg);
            }
        }

        public void Publish(string key)
        {
            if (TryGetStringKeyedDelegate<Action>(key, out var handler))
                handler.Invoke();
        }

        public void Publish<T1>(string key, T1 arg1)
        {
            if (TryGetStringKeyedDelegate<Action<T1>>(key, out var handler))
                handler.Invoke(arg1);
        }

        public void Publish<T1, T2>(string key, T1 arg1, T2 arg2)
        {
            if (TryGetStringKeyedDelegate<Action<T1, T2>>(key, out var handler))
                handler.Invoke(arg1, arg2);
        }

        public void Publish<T1, T2, T3>(string key, T1 arg1, T2 arg2, T3 arg3)
        {
            if (TryGetStringKeyedDelegate<Action<T1, T2, T3>>(key, out var handler))
                handler.Invoke(arg1, arg2, arg3);
        }

        public void Publish<T1, T2, T3, T4>(string key, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (TryGetStringKeyedDelegate<Action<T1, T2, T3, T4>>(key, out var handler))
                handler.Invoke(arg1, arg2, arg3, arg4);
        }

        // ---------------------------------------------------------------------
        // Служебное
        // ---------------------------------------------------------------------

        /// <summary>Полностью удаляет ключ (все сигнатуры) из шины.</summary>
        public void Clear(string key) => _stringKeyedEvents.Remove(key);

        public void Clear(Type key) => _typeKeyedEvents.Remove(key);

        /// <summary>Полностью очищает шину.</summary>
        public void ClearAll()
        {
            _stringKeyedEvents.Clear();
            _typeKeyedEvents.Clear();
        }

        private void AddTypeKeyedHandler(Type key, Delegate handler)
        {
            _typeKeyedEvents.TryGetValue(key, out var existing);
            _typeKeyedEvents[key] = Delegate.Combine(existing, handler);
        }

        private void RemoveTypeKeyedHandler(Type key, Delegate handler)
        {
            if (_typeKeyedEvents.TryGetValue(key, out var existing))
            {
                var result = Delegate.Remove(existing, handler);
                if (result == null)
                {
                    _typeKeyedEvents.Remove(key);
                }
                else
                {
                    _typeKeyedEvents[key] = result;
                }
            }
        }

        private void AddStringKeyedHandler(string key, Delegate handler)
        {
            if (!_stringKeyedEvents.TryGetValue(key, out var byType))
            {
                byType = new Dictionary<Type, Delegate>();
                _stringKeyedEvents[key] = byType;
            }

            var type = handler.GetType();
            byType.TryGetValue(type, out var existing);
            byType[type] = Delegate.Combine(existing, handler);
        }

        private void RemoveStringTypedHandler(string key, Delegate handler)
        {
            if (!_stringKeyedEvents.TryGetValue(key, out var byType))
                return;

            var type = handler.GetType();
            if (!byType.TryGetValue(type, out var existing))
                return;

            var result = Delegate.Remove(existing, handler);
            if (result == null)
                byType.Remove(type);
            else
                byType[type] = result;

            if (byType.Count == 0)
                _stringKeyedEvents.Remove(key);
        }

        private bool TryGetTypeKeyedDelegate<TDelegate>(Type key, out TDelegate handler)
            where TDelegate : Delegate
        {
            handler = null;

            if (_typeKeyedEvents.TryGetValue(key, out var dDelegate))
            {
                handler = (TDelegate)dDelegate;
                return true;
            }

            return false;
        }

        private bool TryGetStringKeyedDelegate<TDelegate>(string key, out TDelegate handler)
            where TDelegate : Delegate
        {
            handler = null;

            if (_stringKeyedEvents.TryGetValue(key, out var byType)
                && byType.TryGetValue(typeof(TDelegate), out var del))
            {
                handler = (TDelegate)del;
                return true;
            }

            return false;
        }
    }
}