using System;
using System.Collections;
using Core.Graph;
using Core.Graph.Wires;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ButtonDevice : DeviceBase<ActionPort>
    {
        [SerializeField] private Transform button;
        [SerializeField, Range(0, 2.0f)] private float minPos;
        [SerializeField, Range(0, 1.0f)] private float waitButton;

        private Coroutine animClick;

        private void OnClick()
        {
            if(animClick == null)
            {
                animClick = StartCoroutine(AnimButtonClick());
            }
        }

        public override void Init(IGraphHandler graph, IBlock block)
        {
            base.Init(graph, block);
            Port.AddRegisterAction(OnClick);
        }

        private IEnumerator AnimButtonClick()
        {
            button.localPosition = new Vector3(0, -minPos, 0);
            yield return new WaitForSeconds(waitButton);
            button.localPosition = Vector3.zero;
            animClick = null;
        }

        public override void MoveValueInteractive(float val)
        {
        }

        public override void ExitControl()
        {
            Port.Call();
        }

        public override ActionPort Port => port;
        private ActionPort port = new ActionPort();
    }
}