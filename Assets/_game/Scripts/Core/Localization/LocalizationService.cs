using UnityEngine.Localization.Settings;

namespace Core.Localization
{
    public static class LocalizationService
    {
        private const string TABLE_NAME = "LocalizationCollection";
        public static string Localize(string key, params object[] args)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedStringAsync(TABLE_NAME, key, args).WaitForCompletion();
        }
    }
    
}
