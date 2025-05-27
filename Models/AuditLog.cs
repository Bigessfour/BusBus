using System;
using System.Text.Json;

#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return

namespace BusBus.Models
{
    public class AuditLog
    {
        public long AuditID { get; set; }
        public string TableName { get; set; }
        public int RecordID { get; set; }
        public string Operation { get; set; } // INSERT, UPDATE, DELETE
        public string OldValues { get; set; } // JSON
        public string NewValues { get; set; } // JSON
        public string ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }

        // Typed access to JSON values
        public T GetOldValues<T>() where T : class
        {
            return string.IsNullOrEmpty(OldValues) ? null : JsonSerializer.Deserialize<T>(OldValues);
        }

        public T GetNewValues<T>() where T : class
        {
            return string.IsNullOrEmpty(NewValues) ? null : JsonSerializer.Deserialize<T>(NewValues);
        }
    }
}
