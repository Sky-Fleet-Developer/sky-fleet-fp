using Core.Ai;
using Core.Graph.Wires;
using Core.Structure.Rigging;
using Core.Weapon;
using Core.World;
using UnityEngine;
using Zenject;

namespace Runtime.Structure.Rigging.Combat
{
    public class Gun : BlockWithNode, IMenace
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private float menaceAbstractDistance = 500f;
        [Inject] private UnitEntity _myUnit;
        public UnitEntity MyUnit => _myUnit;
        public float MenaceDistanceSqr => menaceAbstractDistance * menaceAbstractDistance;
        public Ray AimingRay => new Ray(muzzle.position, muzzle.forward);
        public float MenaceFactorValue { get; }
    }
}