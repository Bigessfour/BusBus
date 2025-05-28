// This file is used to suppress code analysis warnings across the test project
using System.Diagnostics.CodeAnalysis;

// Platform compatibility warnings - This is a Windows Forms application designed specifically for Windows
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility",
    Justification = "This is a Windows Forms application designed specifically for Windows platform")]

// Test-specific suppressions
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static",
    Justification = "Test methods require instance context for MSTest framework")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Test exception handling may need to catch general exceptions")]

[assembly: SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed",
    Justification = "Test disposables are managed by test framework or using statements")]

// Nullability warnings specific to test context
[assembly: SuppressMessage("Style", "CS8618:Non-nullable property must contain a non-null value when exiting constructor",
    Justification = "Test properties are initialized in test setup methods")]

[assembly: SuppressMessage("Style", "CS8600:Converting null literal or possible null value to non-nullable type",
    Justification = "Test scenarios may intentionally work with null values")]
