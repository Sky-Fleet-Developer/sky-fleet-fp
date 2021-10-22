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

    }
}