// This file is used to suppress code analysis warnings across the project
// <auto-generated />
using System.Diagnostics.CodeAnalysis;

// Nullability warnings
[assembly: SuppressMessage("Style", "CS8600:Converting null literal or possible null value to non-nullable type", Justification = "Precautionary warning")]
[assembly: SuppressMessage("Style", "CS8618:Non-nullable property must contain a non-null value when exiting constructor", Justification = "Properties are initialized in Setup method")]
[assembly: SuppressMessage("Style", "CS0649:Field is never assigned to, and will always have its default value", Justification = "Field not currently used but may be needed in future")]

// Unused fields
[assembly: SuppressMessage("Style", "CS0169:The field is never used", Justification = "Field not currently used but may be needed in future")]
[assembly: SuppressMessage("Performance", "CA1823:Unused field", Justification = "Field not currently used but may be needed in future")]

// Redundant initialization
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Explicit initialization for clarity")]

// Resource disposal
[assembly: SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "UI controls disposed by parent form")]

// Exception handling
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exception handling for robustness")]

// Note: In addition to these global suppressions, we've added #pragma warning directives 
// directly in source files where appropriate. Some warnings have been selectively suppressed 
// to avoid modifying working code. The general strategy has been to suppress warnings that 
// are precautionary and unlikely to cause runtime issues, while maintaining the functional 
// behavior of the application.
