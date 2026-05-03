using System;
using System.Linq;
using Core;
using Core.Ai;
using Core.Misc;
using Core.World;
using Runtime.Ai;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;
using ITickable = Core.Misc.ITickable;

namespace WorldEditor
{
    public class EntitiesViewer : MonoBehaviour, ITickable, IDisposable, IMyInstaller
    {
        [SerializeField] private CinemachineCamera virtualCamera;
        [Inject] private TickService _tickService;
        [Inject] private WorldGrid _worldGrid;
        private Transform _fakeTarget;
        public int TickRate => 1;
        public IWorldEntity _currentEntity;
        private bool _enabled;
        private InputActions _inputActions;

        public IWorldEntity CurrentEntity
        {
            get => _currentEntity;
            set
            {
                if (value != _currentEntity)
                {
                    _currentEntity = value;
                    ViewEntity();
                }
            }
        }

        private void Awake()
        {
            virtualCamera.enabled = false;
            CurrentEntity = null;
            _fakeTarget = new GameObject("FakeTarget").transform;
            _fakeTarget.SetParent(transform);
            _inputActions = new InputActions();
            _inputActions.EntitiesViewer.Enable();
            _inputActions.EntitiesViewer.NextEntity.performed += NextEntity;
            _inputActions.EntitiesViewer.PrevEntity.performed += PrevEntity;
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }
            _tickService.Add(this);
            virtualCamera.enabled = true;
            _enabled = true;
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }
            _tickService.Remove(this);
            virtualCamera.enabled = false;
            _enabled = false;
        }

        private void NextEntity(InputAction.CallbackContext callbackContext)
        {
            foreach (var entity in _worldGrid.Entities)
            {
                if (CurrentEntity == null)
                {
                    CurrentEntity = entity;
                    return;
                }
                if (entity == CurrentEntity)
                {
                    CurrentEntity = null;
                }
            }
        }
        
        private void PrevEntity(InputAction.CallbackContext callbackContext)
        {
            if (CurrentEntity == null)
            {
                CurrentEntity = _worldGrid.Entities.LastOrDefault();
                return;
            }
            IWorldEntity prevEntity = null;
            foreach (var entity in _worldGrid.Entities)
            {
                if (entity == CurrentEntity)
                {
                    CurrentEntity = prevEntity;
                    return;
                }
                prevEntity = entity;
            }
        }

        private void ViewEntity()
        {
            if (CurrentEntity == null)
            {
                Disable();
                return;
            }
            else
            {
                Enable();
            }

            if (CurrentEntity is IObjectEntity objectEntity)
            {
                virtualCamera.Follow = objectEntity.GameObject.transform;
            }
            else
            {
                _fakeTarget.position = CurrentEntity.Position;
                virtualCamera.Follow = _fakeTarget;
            }
        }
        
        public void Tick()
        {
            if (CurrentEntity is not IObjectEntity)
            {
                _fakeTarget.position = CurrentEntity.Position;
            }
        }
        private const float Width = 500;
        private const float Height = 500;
        private void OnGUI()
        {
            if (CurrentEntity != null)
            {
                GUI.skin.label.fontSize = 20;
                GUILayout.BeginArea(new Rect(Screen.width - Width - 50, 50, Width, Height));
                GUILayout.Label(CurrentEntity.ToString());
                GUILayout.Label($"Cell: {_worldGrid.Grid.PositionToCell(CurrentEntity.Position).ToString("F1")}");
                if (CurrentEntity is UnitEntity unitEntity)
                {
                    GUILayout.Label($"Tactic: {unitEntity.GetTactic()?.GetType().Name ?? "none"}");
                    if (unitEntity.GetTactic() is DirectAttackTactic directAttackTactic)
                    {
                        GUILayout.Label($"    {directAttackTactic.GetDescription()}");
                    }
                    var maneuver = unitEntity.Unit.CurrentManeuver;
                    GUILayout.Label($"Maneuver: {maneuver?.GetType().Name ?? "none"}");
                    if (unitEntity.Unit.Sensor.Menaces is { Count: > 0 })
                    {
                        GUILayout.Label($"Menace: {unitEntity.Unit.Sensor.Menaces[0].ToString()}");
                    }
                }
                GUILayout.EndArea();
            }
        }

        public void Dispose()
        {
            Disable();
            _inputActions.EntitiesViewer.Disable();
            _inputActions.EntitiesViewer.NextEntity.performed -= NextEntity;
            _inputActions.EntitiesViewer.PrevEntity.performed -= PrevEntity;
        }

        public void InstallBindings(DiContainer container)
        {
            container.Bind<EntitiesViewer>().FromInstance(this).AsSingle();
        }
    }
}