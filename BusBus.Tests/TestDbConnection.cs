using Microsoft.Data.SqlClient;
using System;

namespace BusBus.Tests
{
    class DatabaseConnectionTest
    {
        public static void TestConnection()
        {
            string connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=BusBusDB;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=True";

            Console.WriteLine("Testing LocalDB connection...");
            Console.WriteLine($"Connection string: {connectionString}");

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    Console.WriteLine("Opening connection...");
                    connection.Open();
                    Console.WriteLine("✅ Connection successful!");

                    // Test a simple query
                    using (var command = new SqlCommand("SELECT @@VERSION", connection))
                    {
                        var result = command.ExecuteScalar();
                        Console.WriteLine($"SQL Server version: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");

                if (ex.Message.Contains("cannot be opened"))
                {
                    Console.WriteLine("\n🔧 Trying to create database first...");

                    try
                    {
                        // Try connecting to master database first
                        string masterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Integrated Security=true;MultipleActiveResultSets=true;TrustServerCertificate=True";
                        using (var masterConnection = new SqlConnection(masterConnectionString))
                        {
                            masterConnection.Open();
                            Console.WriteLine("✅ Connected to master database");

                            // Create BusBusDB database
                            using (var command = new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'BusBusDB') CREATE DATABASE BusBusDB", masterConnection))
                            {
                                command.ExecuteNonQuery();
                                Console.WriteLine("✅ BusBusDB database created");
                            }
                        }

                        // Now try connecting to BusBusDB again
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();
                            Console.WriteLine("✅ Successfully connected to BusBusDB!");
                        }
                    }
                    catch (Exception createEx)
                    {
                        Console.WriteLine($"❌ Failed to create database: {createEx.Message}");
                    }
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
