using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Core.Utilities.AsyncAwaitUtil.Source
{
    public class WaitForBackgroundThread
    {
        public ConfiguredTaskAwaitable.ConfiguredTaskAwaiter GetAwaiter()
        {
            return Task.Run(() => {}).ConfigureAwait(false).GetAwaiter();
        }
    }
}
