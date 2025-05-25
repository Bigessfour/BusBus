using BusBus.Models;
using BusBus.UI.Common;
using System;

namespace BusBus.Services
{
    /// <summary>
    /// Service interface for managing vehicles in the BusBus system
    /// </summary>
    public interface IVehicleService : ICrudService<Vehicle, Guid>
    {
        // Additional vehicle-specific methods can be added here
    }
}
