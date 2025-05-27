// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Generic CRUD service interface for any entity type
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public interface ICrudService<T, TKey> where T : class
    {
        /// <summary>
        /// Creates a new entity
        /// </summary>
        Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paginated list of entities
        /// </summary>
        Task<List<T>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total count of entities
        /// </summary>
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing entity
        /// </summary>
        Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes an entity by its ID
        /// </summary>
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates an entity before create/update operations
        /// </summary>
        /// <returns>Tuple of (IsValid, ErrorMessage)</returns>
        (bool IsValid, string ErrorMessage) ValidateEntity(T entity);
    }
}
