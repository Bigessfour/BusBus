#nullable enable
namespace BusBus.UI
{
    public interface IStatefulView
    {
        void SaveState(object state);
        void RestoreState(object state);
    }
}
