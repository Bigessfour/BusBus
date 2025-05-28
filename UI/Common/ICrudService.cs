// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Generic interface for CRUD operations
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's key type</typeparam>
    public interface ICrudService<TEntity, TKey> where TEntity : class
    {
        Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
        Task<List<TEntity>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(CancellationToken cancellationToken = default);
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
        (bool IsValid, string ErrorMessage) ValidateEntity(TEntity entity);
    }
}
