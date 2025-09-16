using System;
using System.Collections.Generic;

namespace Core.UIStructure.Utilities
{
    public class ListSelectionHandler<TTarget> : IDisposable where TTarget : class, ISelectionTarget
    {
        private List<TTarget> _targets = new();
        private List<ISelectionListener<TTarget>> _listeners = new();
        private TTarget _selected;
        public TTarget Selected => _selected;
        public IEnumerable<TTarget> Targets => _targets;
        
        public void AddTarget(TTarget target)
        {
            _targets.Add(target);
            target.OnSelected += OnSelected;
        }

        public void RemoveTarget(TTarget target, bool deselectIfSelected = true)
        {
            if (deselectIfSelected && Equals(_selected, target))
            {
                OnSelected(null);
            }
            _targets.Remove(target);
            target.OnSelected -= OnSelected;
        }

        public void ClearTargets()
        {
            if (_selected != null)
            {
                var prev = _selected;
                _selected = null;
                prev.Deselected();
                for (var i = 0; i < _listeners.Count; i++)
                {
                    _listeners[i].OnSelectionChanged(prev, _selected);
                }
            }

            foreach (var selectionTarget in _targets)
            {
                selectionTarget.OnSelected -= OnSelected;
            }
            _targets.Clear();
        }
        
        private void OnSelected(ISelectionTarget value)
        {
            var prev = _selected;
            _selected = (TTarget)value;
            prev?.Deselected();
            _selected?.Selected();
            for (var i = 0; i < _listeners.Count; i++)
            {
                _listeners[i].OnSelectionChanged(prev, _selected);
            }
        }
        
        public void AddListener(ISelectionListener<TTarget> listener)
        {
            _listeners.Add(listener);
        }
        public void RemoveListener(ISelectionListener<TTarget> listener)
        {
            _listeners.Remove(listener);
        }

        public void Dispose()
        {
            for (var i = 0; i < _targets.Count; i++)
            {
                _targets[i].OnSelected -= OnSelected;
            }
            _targets.Clear();
            _targets = null;
            _listeners.Clear();
            _listeners = null;
        }
    }
}