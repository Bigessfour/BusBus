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
