using System;
using System.Collections;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Structure.Rigging.Control.Attributes;
using UnityEngine;

namespace Runtime.Structure.Rigging.Control.Attributes
{
    public class ButtonDevice : DeviceBase<Action<object>>
    {
        [SerializeField] private Transform button;
        [SerializeField, Range(0, 2.0f)] private float minPos;
        [SerializeField, Range(0, 1.0f)] private float waitButton;

        private Coroutine animClick;

        private void OnClick(object sender)
        {
            if(animClick == null)
            {
                animClick = StartCoroutine(AnimButtonClick());
            }
        }

        public override void Init(IStructure structure, IBlock block)
        {
            base.Init(structure, block);
            port.Value += OnClick;
        }

        private IEnumerator AnimButtonClick()
        {
            button.localPosition = new Vector3(0, -minPos, 0);
            yield return new WaitForSeconds(waitButton);
            button.localPosition = Vector3.zero;
            animClick = null;
        }
    }
}