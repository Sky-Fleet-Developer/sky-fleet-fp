using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.UIStructure.Utilities
{
    public class MultipleSelectionHandler<TTarget> : IDisposable where TTarget : class, IMultipleSelectionTarget
    {
        private List<TTarget> _targets = new ();
        private List<IMultipleSelectionListener<TTarget>> _listeners = new();
        private List<TTarget> _selectedA = new (10);
        private List<TTarget> _selectedB = new (10);
        private bool _selectedToggle;
        private TTarget _lastInput;
        private List<TTarget> CurrentSelected => _selectedToggle ? _selectedA : _selectedB;
        private List<TTarget> OtherBuffer => _selectedToggle ? _selectedB : _selectedA;
        private TTarget LastInput => _lastInput;

        public IEnumerable<TTarget> Targets => _targets;
        
        public void AddTarget(TTarget target)
        {
            _targets.Add(target);
            _targets.Sort((a, b) => a.Order.CompareTo(b.Order));
            target.OnInput += OnInput;
        }

        public void RemoveTarget(TTarget target, bool deselectIfSelected = true)
        {
            if (deselectIfSelected)
            {
                var selected = CurrentSelected.IndexOf(target);
                if (selected <= 0)
                {
                    target.Deselected();
                    CurrentSelected.RemoveAt(selected);
                }
            }
            _targets.Remove(target);
            target.OnInput -= OnInput;
        }

        public void ClearTargets()
        {
            foreach (var selected in CurrentSelected)
            {
                selected.Deselected();
                for (var i = 0; i < _listeners.Count; i++)
                {
                    _listeners[i].OnDeselected(selected);
                }
            }
            CurrentSelected.Clear();

            UnsubscribeAll();
            _targets.Clear();
        }

        private void UnsubscribeAll()
        {
            foreach (var selectionTarget in Targets)
            {
                selectionTarget.OnInput -= OnInput;
            }
        }

        private void OnInput(IMultipleSelectionTarget value, MultipleSelectionModifiers modifiers)
        {
            var last = _lastInput;
            _lastInput = (TTarget)value;
            if (last != null && modifiers.HasFlag(MultipleSelectionModifiers.Shift))
            {
                SelectRange(last, value, !modifiers.HasFlag(MultipleSelectionModifiers.Ctrl));
                return;
            }

            bool isAlreadySelected = false;
            foreach (var target in CurrentSelected)
            {
                if (target == value)
                {
                    isAlreadySelected = true;
                }
                else
                {
                    Deselect(target);
                }
            }

            if (!isAlreadySelected)
            {
                Select(_lastInput);
                FinalSelect(_lastInput);
            }
            else
            {
                Deselect(_lastInput);
            }
            Swap();
        }

        private void SelectRange(IMultipleSelectionTarget a, IMultipleSelectionTarget b, bool clearLast)
        {
            bool isFirstAlreadySelected = false;
            if (clearLast)
            {
                foreach (var target in CurrentSelected)
                {
                    if (target != a)
                    {
                        Deselect(target);
                    }
                    else
                    {
                        isFirstAlreadySelected = true;
                    }
                }
            }
            else
            {
                foreach (var target in CurrentSelected)
                {
                    if (target == a)
                    {
                        isFirstAlreadySelected = true;
                    }
                }
            }
            ProcessRange(a, b, !isFirstAlreadySelected, true);
            FinalSelect(b);
        }

        private void FinalSelect(IMultipleSelectionTarget b)
        {
            foreach (IMultipleSelectionListener<TTarget> listener in _listeners)
            {
                listener.OnFinalSelected((TTarget)b);
            }
        }

        private void ProcessRange(IMultipleSelectionTarget a, IMultipleSelectionTarget b, bool needProcessFirst, bool valueToSet)
        {
            int aOrder = a.Order;
            int bOrder = b.Order;
            int current = _targets.Count / 2, step = Mathf.Max(current / 2, 1);
            int counter = 0;
            while (_targets[current].Order != aOrder)
            {
                current += aOrder > _targets[current].Order ? step : -step;
                step /= 2;
                if (counter++ == 1000)
                {
                    throw new Exception();
                }
            }

            if (needProcessFirst)
            {
                Process(_targets[current]);
            }

            step = bOrder > _targets[current].Order ? 1 : -1;
            while (_targets[current].Order != bOrder)
            {
                current += step;
                Process(_targets[current]);
                if (counter++ == 1000)
                {
                    throw new Exception();
                }
            }

            void Process(TTarget value)
            {
                if (valueToSet)
                {
                    Select(value);
                }
                else
                {
                    Deselect(value);
                }
            }
        }

        private void Swap()
        {
            _selectedToggle = !_selectedToggle;
            OtherBuffer.Clear();
            foreach (var target in CurrentSelected)
            {
                OtherBuffer.Add(target);
            }
        }

        private void Deselect(TTarget target)
        {
            OtherBuffer.Remove(target);
            target.Deselected();
            for (var i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnDeselected(target);
            }
        }
        private void Select(TTarget target)
        {
            OtherBuffer.Add(target);
            target.Selected();
            for (var i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnSelected(target);
            }
        }
        
        public void AddListener(IMultipleSelectionListener<TTarget> listener)
        {
            _listeners.Add(listener);
        }
        public void RemoveListener(IMultipleSelectionListener<TTarget> listener)
        {
            _listeners.Remove(listener);
        }

        public void Dispose()
        {
            UnsubscribeAll();
            _targets = null;
            _listeners.Clear();
            _listeners = null;
        }
    }
}