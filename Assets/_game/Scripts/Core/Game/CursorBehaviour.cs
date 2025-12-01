using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Boot_strapper;
using Core.Patterns.State;
using UnityEngine;

namespace Core.Game
{
    public class CursorBehaviour : MonoBehaviour, ILoadAtStart, IStateMaster
    {
        public static CursorBehaviour Instance;
        public static bool RotationLocked;
        public IState CurrentState { get; set; }
        public event Action StateChanged;
        private bool _isActive;
        bool ILoadAtStart.enabled => _isActive;

        private void Awake()
        {
            if (_isActive) return;
            _isActive = true;
            gameObject.SetActive(false);
        }

        public void SetStatePrivate(IState value)
        {
            CurrentState = value;
        }

        public Task Load()
        {
            _isActive = true;
            Instance = this;
            gameObject.SetActive(true);
            CurrentState = new FreeCursorState(this);
            return Task.CompletedTask;
        }

        private void Update()
        {
            CurrentState.Run();
        }

        public static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

        public static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void SetAimingState()
        {
            Instance.CurrentState = new AimingCursorState(Instance, (IState<CursorBehaviour>)Instance.CurrentState);
        }

        public static void ExitAimingState()
        {
            ((AimingCursorState)Instance.CurrentState).ExitState();
        }

        private void OnDestroy()
        {
            UnlockCursor();
        }


        public class FreeCursorState : IState<CursorBehaviour>
        {
            public CursorBehaviour Master { get; }

            public FreeCursorState(CursorBehaviour master)
            {
                Master = master;
            }
            
            public void Run()
            {
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    RotationLocked = true;
                    UnlockCursor();
                }

                if (Input.GetKeyUp(KeyCode.LeftControl))
                {
                    RotationLocked = false;
                    LockCursor();
                }
            }
        }

        public class AimingCursorState : IState<CursorBehaviour>
        {
            private IState<CursorBehaviour> _lastState;
            public CursorBehaviour Master { get; }

            public AimingCursorState(CursorBehaviour master, IState<CursorBehaviour> lastState)
            {
                Master = master;
                _lastState = lastState;
                RotationLocked = true;
                LockCursor();
            }

            public void ExitState()
            {
                RotationLocked = false;
                Master.CurrentState = _lastState;
            }
            
            public void Run()
            {
            }
        }

    }
}