using System.Threading.Tasks;

namespace BusBus.UI
{
    public interface IStatefulView
    {
        void SaveState(object state);
        void RestoreState(object state);
        object? GetState();
        Task ActivateAsync();
        Task DeactivateAsync();
    }
}
