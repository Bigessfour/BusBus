#nullable enable
using System;

namespace BusBus.UI
{
    public class StatusEventArgs : EventArgs
    {
        public string Message { get; }
        public StatusType Type { get; }

        public StatusEventArgs(string message, StatusType type = StatusType.Info)
        {
            Message = message;
            Type = type;
        }
    }
}
