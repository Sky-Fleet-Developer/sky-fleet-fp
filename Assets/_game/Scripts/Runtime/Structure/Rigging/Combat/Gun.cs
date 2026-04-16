using System.Collections.Generic;
using System.Linq;
using Core.Ai;
using Core.Graph.Wires;
using Core.Items;
using Core.Structure;
using Core.Structure.Rigging;
using Core.Trading;
using Core.Weapon;
using Core.World;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Runtime.Structure.Rigging.Combat
{
    public class Gun : BlockWithNode, IMenace, IKineticWeapon
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private float menaceAbstractDistance = 500f;
        [Inject] private UnitEntity _myUnit;
        [Inject] private ProjectileHandler _projectileHandler;
        [Inject] private BankSystem _bankSystem;
        public UnitEntity MyUnit => _myUnit;
        public float MenaceDistanceSqr => menaceAbstractDistance * menaceAbstractDistance;
        public Ray AimingRay => new Ray(muzzle.position, muzzle.forward);
        public float MenaceFactorValue { get; }
        public Transform Muzzle => muzzle;
        public Vector3 Velocity => Structure is IDynamicStructure dynamicStructure ? dynamicStructure.GetPointVelocity(muzzle.position) : Vector3.zero;

        private ActionPort shootInput = new();
        private IItemInstancesSource _inventory;
        private ItemInstance _shell;

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            shootInput.RegisterAction(Shoot);
            _inventory = _bankSystem.GetPullPutWarp(SourceItem.ContainerKey);
            RefreshShell();
        }

        private void RefreshShell()
        {
            _shell = _inventory.GetItems().FirstOrDefault(x => x.Sign.HasTag(ItemSign.ShellTag));
        }

        private void Shoot()
        {
            while (_shell == null || _shell.Amount <= 0)
            {
                RefreshShell();
                if (_shell == null)
                {
                    return;
                }
            }
            
            if (_inventory.TryPullItem(_shell, 1, out var projectile))
            {
                _projectileHandler.MakeProjectile(this, projectile);
            }
        }
    }
}