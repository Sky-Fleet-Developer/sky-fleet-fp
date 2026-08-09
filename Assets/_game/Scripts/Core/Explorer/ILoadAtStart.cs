using System.Threading.Tasks;

namespace Core.Explorer
{
    public interface ILoadAtStart
    {
        bool enabled { get; }
        Task Load();
    }
}