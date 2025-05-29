using System;
using System.Threading.Tasks;

namespace BusBus.Data
{
    /// <summary>
    /// Interface for database management operations
    /// </summary>
    public interface IDatabaseManager
    {
        /// <summary>
        /// Checks if the database is connected
        /// </summary>
        /// <returns>True if connected, false otherwise</returns>
        Task<bool> IsConnectedAsync();

        /// <summary>
        /// Gets the last successful connection time
        /// </summary>
        /// <returns>DateTime of the last successful connection</returns>
        Task<DateTime> GetLastConnectionTimeAsync();

        /// <summary>
        /// Initializes the database
        /// </summary>
        Task InitializeDatabaseAsync();
    }
}
