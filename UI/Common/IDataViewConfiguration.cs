#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Configuration interface for dynamic DataGridView views in the DashboardView
    /// Provides a contract for defining how different entity types (Routes, Drivers, Vehicles)
    /// should be displayed and managed within the shared DataGridView component
    /// </summary>
    public interface IDataViewConfiguration<T> where T : class
    {
        /// <summary>
        /// Display name for this view type (e.g., "Routes", "Drivers", "Vehicles")
        /// </summary>
        string ViewName { get; }

        /// <summary>
        /// Plural display name for this view type (e.g., "Routes", "Drivers", "Vehicles")
        /// </summary>
        string PluralName { get; }

        /// <summary>
        /// Icon to display for this view type (emoji or unicode character)
        /// </summary>
        string Icon { get; }

        /// <summary>
        /// Configures the DataGridView columns for this entity type
        /// </summary>
        /// <param name="dataGrid">The DataGridView to configure</param>
        void ConfigureColumns(DataGridView dataGrid);

        /// <summary>
        /// Loads data for this view with pagination support
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of entities for the requested page</returns>
        Task<List<T>> LoadDataAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of entities for pagination calculations
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Total number of entities</returns>
        Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new entity
        /// </summary>
        /// <param name="entity">The entity to create</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The created entity</returns>
        Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        /// <param name="entity">The entity to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The updated entity</returns>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the unique identifier for an entity
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The entity's unique identifier</returns>
        object GetEntityId(T entity);

        /// <summary>
        /// Creates a new instance of the entity for the "Create" operation
        /// </summary>
        /// <returns>A new entity instance with default values</returns>
        T CreateNewEntity();
    }
}
