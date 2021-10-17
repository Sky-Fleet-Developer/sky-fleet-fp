using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ButtonDevice : EasyDevice<Action<object>>
    {
        [SerializeField, Range(0, 2.0f)] private float minPos;
        [SerializeField, Range(0, 1.0f)] private float waitButton;

        private Coroutine animClick;

        private void OnClick(object sender)
        {
            if(animClick == null && IsMinLod)
            {
                animClick = StartCoroutine(AnimButtonClick());
            }
        }

        public override void Init(IStructure structure, IBlock block, string port)
        {
            base.Init(structure, block, port);
            wire.value += OnClick;
        }

        public override void UpdateDevice(int lod)
        {
            base.UpdateDevice(lod);
        }

        IEnumerator AnimButtonClick()
        {
            visualTransfrom.localPosition = new Vector3(0, -minPos, 0);
            yield return new WaitForSeconds(waitButton);
            visualTransfrom.localPosition = Vector3.zero;
            animClick = null;
            yield break;
        }
    }
}