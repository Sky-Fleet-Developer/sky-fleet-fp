using Core;
using Core.Trading;
using UnityEngine;
using Zenject;

namespace Runtime.Trading
{
    public class WeightAndVolumeCalculatorInstaller : MonoBehaviour, IMyInstaller
    {
        private MassAndVolumeCalculator _massAndVolumeCalculator;

        [Inject]
        private void Inject(DiContainer container)
        {
            container.Inject(_massAndVolumeCalculator);
        }
        
        public void InstallBindings(DiContainer container)
        {
            _massAndVolumeCalculator = new MassAndVolumeCalculator();
            container.Bind<IMassAndVolumeCalculator>().To<MassAndVolumeCalculator>()
                .FromInstance(_massAndVolumeCalculator);
        }
    }
}