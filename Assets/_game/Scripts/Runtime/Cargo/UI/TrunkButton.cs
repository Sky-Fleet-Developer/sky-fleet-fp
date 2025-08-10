using Core.Structure.Rigging.Cargo;

namespace Runtime.Cargo.UI
{
    public class TrunkButton : CargoLoadingButton
    {
        private ICargoTrunkPlayerInterface _target;
        public ICargoTrunkPlayerInterface Data => _target;

        public void SetTarget(ICargoTrunkPlayerInterface data)
        {
            _target = data;
            SetTrackingTarget(_target.transform);
        }
    }
}