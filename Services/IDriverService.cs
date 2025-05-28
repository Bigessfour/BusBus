using BusBus.Models;
using BusBus.UI.Common;
using System;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing drivers in the BusBus system
    /// </summary>
    public interface IDriverService : ICrudService<Driver, Guid>
    {
        // Additional driver-specific methods can be added here if needed
    }
}
