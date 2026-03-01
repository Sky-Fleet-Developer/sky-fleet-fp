using Core.Configurations;

namespace Runtime.Cargo.UI
{
    public class CargoButton : CargoLoadingButton
    {
        private IRemotePrefab _target;
        public IRemotePrefab Data => _target;

        public void SetTarget(IRemotePrefab data)
        {
            _target = data;
            SetTrackingTarget(_target.transform);
        }
    }
}