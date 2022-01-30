using System.Threading.Tasks;

namespace Core.Boot_strapper
{
    public interface ILoadAtStart
    {
        Task LoadStart();
    }
}