using System;
using System.Collections.Generic;
using System.Linq;
using Core.Ai;
using Core.Character.Interaction;
using Core.Configurations;
using Core.Graph.Wires;
using Core.Items;
using Core.Misc;
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
    public class Gun : BlockWithNode, IMenace, IKineticWeapon, IWeaponHandler
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private float menaceAbstractDistance = 500f;
        [Inject] private UnitEntity _myUnit;
        [Inject] private ProjectileHandler _projectileHandler;
        [Inject] private BankSystem _bankSystem;
        [Inject] private ItemsTable _itemsTable;
        [Inject] private MenacesWatcher _menacesWatcher;
        [Inject] private TransformCacheSystem _transformCacheSystem;
        // ReSharper disable once InconsistentNaming
        private ActionPort shootInput = new();
        private IItemInstancesSource _inventory;
        private ItemInstance _shell;
        private float _menaceFactor;
        private CaliberSign _myCaliber;
        private bool _isRegisteredInMenacesWatcher = false;
        private Vector3 _muzzleLocalPos;
        private Quaternion _muzzleLocalRot;
                
        public UnitEntity MyUnit => _myUnit;
        public float MenaceDistanceSqr => menaceAbstractDistance * menaceAbstractDistance;
        public Ray AimingRay => new Ray(_myUnit.GetGlobalPositionThreadSafe(_muzzleLocalPos), _myUnit.GetGlobalRotationThreadSafe(_muzzleLocalRot) * Vector3.forward);
        public float MenaceFactorValue => _menaceFactor;
        public Transform Muzzle => muzzle;
        public Vector3 Velocity => Structure is IDynamicStructure dynamicStructure ? dynamicStructure.GetPointVelocity(muzzle.position) : Vector3.zero;
        public float Accuracy => 1f;
        public bool CanAimHorizontally => false;
        public bool CanAimVertically => false;
        public float HorizontalAimAxis { get; set; }
        public float VerticalAimAxis { get; set; }
        public TransformCache MuzzleThreadSafe => _transformCacheSystem.Read(muzzle);

        public override void InitBlock(IStructure structure, Parent parent)
        {
            base.InitBlock(structure, parent);
            shootInput.RegisterAction(Shoot);
            _inventory = _bankSystem.GetPullPutWarp(SourceItem.ContainerKey);
            RefreshShell();
            _muzzleLocalPos = structure.transform.InverseTransformPoint(muzzle.position);
            _muzzleLocalRot = Quaternion.Inverse(structure.transform.rotation) * muzzle.rotation;
            _transformCacheSystem.AddTarget(muzzle);
        }
        
        protected override void OnItemSet()
        {
            base.OnItemSet();
            var myWeapon = _itemsTable.GetKineticWeapon(SourceItem.Sign.Id);
            _myCaliber = myWeapon.caliber;
            _menaceFactor = _myCaliber.DiameterMeters;
            TryRegisterMenace();
        }

        private void TryRegisterMenace()
        {
            if (!_isRegisteredInMenacesWatcher)
            {
                _menacesWatcher.RegisterMenace(this);
                _isRegisteredInMenacesWatcher = true;
            }
        }

        private void OnEnable()
        {
            if (_menacesWatcher != null)
            {
                TryRegisterMenace();
            }
        }

        private void OnDisable()
        {
            if (_isRegisteredInMenacesWatcher)
            {
                _menacesWatcher.UnregisterMenace(this);
                _isRegisteredInMenacesWatcher = false;
            }
        }

        private void RefreshShell()
        {
            _shell = _inventory.GetItems().FirstOrDefault(x => x.Sign.HasTag(ItemSign.ShellTag) && _itemsTable.GetShell(x.Sign.Id).caliber.Equals(_myCaliber));
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
        
        void IWeaponHandler.Fire()
        {
            Shoot();
        }

        void IWeaponHandler.ResetControls()
        {
        }
    }
}