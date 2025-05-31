namespace BusBus.UI.Interfaces;

public interface IStatefulView : IView
{
    object? GetState();
    void SetState(object? state);
}
