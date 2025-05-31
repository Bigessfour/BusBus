namespace BusBus.UI.Interfaces;

public enum StatusType
{
    Info,
    Warning,
    Error,
    Success
}

public class StatusEventArgs : EventArgs
{
    public StatusType Status { get; set; }
    public string Message { get; set; }
    public StatusEventArgs(StatusType status, string message)
    {
        Status = status;
        Message = message;
    }
}
