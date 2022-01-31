using System.Threading.Tasks;

namespace Core.Boot_strapper
{
    public interface ILoadAtStart
    {
        bool enabled { get; }
        Task Load();
    }
}