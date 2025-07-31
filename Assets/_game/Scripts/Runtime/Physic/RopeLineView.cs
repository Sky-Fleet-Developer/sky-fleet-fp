using Core;
using UnityEngine;

namespace Runtime.Physic
{
    public class RopeLineView : MonoBehaviour
    {
        [SerializeField] private Rope rope;
        [SerializeField] private LineRenderer lineRenderer;

        private void Awake()
        {
            enabled = false;
            rope.OnInitialize.Subscribe(() =>
            {
                enabled = true;
                lineRenderer.positionCount = 2;
            });
        }

        private void Update()
        {
            lineRenderer.SetPosition(1, rope.GetHookPoint());
        }
    }
}