using Core.Configurations;

namespace Runtime.Cargo.UI
{
    public class CargoButton : CargoLoadingButton
    {
        private ITablePrefab _target;
        public ITablePrefab Data => _target;

        public void SetTarget(ITablePrefab data)
        {
            _target = data;
            SetTrackingTarget(_target.transform);
        }
    }
}