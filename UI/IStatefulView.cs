using System.Threading.Tasks;

namespace BusBus.UI
{
    public interface IStatefulView : IView // Inherit from IView
    {
        void SaveState(object state);
        void RestoreState(object state);
        object? GetState();
        // ActivateAsync and DeactivateAsync are inherited from IView
    }
}
