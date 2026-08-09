using Core.Misc;

namespace Core.Character.Interaction
{
    public interface IWeaponHandler : ICharacterHandler
    {
        public float Accuracy { get; }
        public bool CanAimHorizontally {get;}
        public bool CanAimVertically {get;}
        public float HorizontalAimAxis { get; set; }
        public float VerticalAimAxis { get; set; }
        public TransformCache MuzzleThreadSafe { get; }
        public void Fire();
        public void ResetControls();
    }
}