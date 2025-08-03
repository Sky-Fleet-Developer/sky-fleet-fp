using System;
using System.Collections.Generic;

namespace Core.UIStructure.Utilities
{
    public class ListSelectionHandler<TTarget> : IDisposable where TTarget : ISelectionTarget
    {
        private List<TTarget> _targets = new();
        private List<ISelectionListener<TTarget>> _listeners = new();
        private TTarget _selected;
        public TTarget Selected => _selected;
        public void AddTarget(TTarget target)
        {
            _targets.Add(target);
            target.OnSelected += OnSelected;
        }

        public void RemoveTarget(TTarget target)
        {
            _targets.Remove(target);
            target.OnSelected -= OnSelected;
        }
        
        private void OnSelected(ISelectionTarget value)
        {
            for (var i = 0; i < _listeners.Count; i++)
            {
                var prev = _selected;
                _selected = (TTarget)value;
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

    
    public interface ISelectionTarget
    {
        Action<ISelectionTarget> OnSelected { get; set; }
    }
    
    public interface ISelectionListener<in TTarget>  where TTarget : ISelectionTarget
    {
        void OnSelectionChanged(TTarget prev, TTarget next);
    }
}