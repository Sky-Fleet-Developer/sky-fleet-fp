namespace Core.Data.GameSettings
{
    [System.Serializable]
    public class Settings
    {
        public ControlSettings Control => _control;

        private ControlSettings _control;

        public Settings()
        {
            _control = ControlSettings.GetDefaultSetting();
        }
    }
}