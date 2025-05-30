using System;

namespace BusBus.UI
{
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; }
        public StatusType Type { get; }

        public StatusEventArgs(string message, StatusType type)
        {
            Message = message;
            Type = type;
        }
    }
}
