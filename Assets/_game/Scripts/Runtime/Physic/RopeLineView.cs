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
                lineRenderer.positionCount = rope.LinksCount;
            });
        }

        private void Update()
        {
            int pointer = 0;
            foreach (var point in rope.GetJointsPoints())
            {
                lineRenderer.SetPosition(pointer++, point);
            }
        }
    }
}