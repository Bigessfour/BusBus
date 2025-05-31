using Microsoft.VisualStudio.TestTools.UnitTesting;

// Enable parallel test execution for MSTest
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.ClassLevel)]
