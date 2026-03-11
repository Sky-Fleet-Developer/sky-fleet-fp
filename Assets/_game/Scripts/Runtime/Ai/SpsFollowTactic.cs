using System.Collections.Generic;
using Core.Ai;
using Runtime.Ai.Maneuvers;
using Runtime.Misc;
using UnityEngine;

namespace Runtime.Ai
{
    public class SpsFollowTactic : IUnitTactic
    {
        private SpsViewRange _spline;
        private Dictionary<int, int> _splineLinks;

        public void SetSpline(SpsViewRange spline)
        {
            _spline = spline;
        }

        public void SetSplineLinks(Dictionary<int, int> splineLinks)
        {
            _splineLinks = splineLinks;
        }

        public void ControlUnit(IUnit unit, Sensor sensor)
        {
            if (unit.IsManeuversDone)
            {
                var m = (new Follow(new SpsPointAsTarget(_splineLinks[unit.EntityId], _spline.Spline), Vector3.zero), InfiniteManeuver.Instance);
                unit.SetManeuvers(new (IManeuver, IManeuverEndpoint)[]{m});
            }
        }
    }
}