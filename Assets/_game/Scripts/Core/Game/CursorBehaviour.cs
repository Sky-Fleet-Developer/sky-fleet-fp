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
        public State CurrentState { get; set; }
        public void SetStatePrivate(State value)
        {
            CurrentState = value;
        }

        public Task Load()
        {
            Instance = this;
            CurrentState = new FreeCursorState(this);
            return Task.CompletedTask;
        }

        private void Update()
        {
            CurrentState.Update();
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public static void SetAimingState()
        {
            Instance.CurrentState = new AimingCursorState(Instance, (State<CursorBehaviour>)Instance.CurrentState);
        }

        public static void ExitAimingState()
        {
            ((AimingCursorState)Instance.CurrentState).ExitState();
        }

        private void OnDestroy()
        {
            UnlockCursor();
        }


        public class FreeCursorState : State<CursorBehaviour>
        {
            public FreeCursorState(CursorBehaviour master) : base(master)
            {
            }
            
            public override void Update()
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

        public class AimingCursorState : State<CursorBehaviour>
        {
            private State<CursorBehaviour> _lastState;

            public AimingCursorState(CursorBehaviour master, State<CursorBehaviour> lastState) : base(master)
            {
                _lastState = lastState;
                RotationLocked = true;
                LockCursor();
            }

            public void ExitState()
            {
                RotationLocked = false;
                Master.CurrentState = _lastState;
            }
            
            public override void Update()
            {
                
            }
        }

    }
}