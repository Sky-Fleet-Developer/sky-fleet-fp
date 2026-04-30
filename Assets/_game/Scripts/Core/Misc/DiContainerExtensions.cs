using System;
using Zenject;

namespace Core.Misc
{
    public static class DiContainerExtensions
    {
        public static void DisposeAll(this DiContainer container)
        {
            container.ResolveAll<IDisposable>().ForEach(x => x.Dispose());
        }
    }
}