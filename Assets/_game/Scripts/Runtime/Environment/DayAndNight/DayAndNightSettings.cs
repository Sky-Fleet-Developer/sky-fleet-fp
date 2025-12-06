using Core;
using UnityEngine;
using Zenject;

namespace Runtime.Environment.DayAndNight
{
    [CreateAssetMenu(fileName = "DayAndNightSettings", menuName = "SF/Env/DayAndNightSettings")]
    public class DayAndNightSettings : ScriptableObject, IMyInstaller
    {
        public SunTravelParameters sunTravelParameters;
        public void InstallBindings(DiContainer container)
        {
            container.BindInstance(this);
        }
    }
}