#pragma warning disable CS8618 // Non-nullable property must contain a non-null value when exiting constructor
#pragma warning disable CS8603 // Possible null reference return

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BusBus.Models
{
    public class CustomField
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text"; // text, date, select, number, bool
        public bool Required { get; set; } = false;
        public string DefaultValue { get; set; } = string.Empty;
        public Collection<string> Options { get; } = new Collection<string>();
    }
}
