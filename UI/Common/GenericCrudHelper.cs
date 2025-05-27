// <auto-added>
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Generic CRUD helper for DataGridView-based entity management
    /// Provides common CRUD operations with consistent error handling and validation
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <typeparam name="TKey">The primary key type</typeparam>
    public class GenericCrudHelper<T, TKey> where T : class
    {
        private readonly ICrudService<T, TKey> _crudService;
        private readonly Func<T, TKey> _getIdFunc;
        private readonly Action<T, TKey> _setIdFunc;

        public GenericCrudHelper(
            ICrudService<T, TKey> crudService,
            Func<T, TKey> getIdFunc,
            Action<T, TKey> setIdFunc)
        {
            _crudService = crudService ?? throw new ArgumentNullException(nameof(crudService));
            _getIdFunc = getIdFunc ?? throw new ArgumentNullException(nameof(getIdFunc));
            _setIdFunc = setIdFunc ?? throw new ArgumentNullException(nameof(setIdFunc));
        }

        /// <summary>
        /// Creates a new entity with validation and error handling
        /// </summary>
        public async Task<T> CreateEntityAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            // Validate the entity
            var (isValid, errorMessage) = _crudService.ValidateEntity(entity);
            if (!isValid)
            {
                ShowError($"Validation failed: {errorMessage}");
                throw new ArgumentException(errorMessage);
            }

            try
            {
                var createdEntity = await _crudService.CreateAsync(entity);
                ShowSuccess("Entity created successfully.");
                return createdEntity;
            }
            catch (Exception ex)
            {
                ShowError($"Error creating entity: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Updates an existing entity with validation and error handling
        /// </summary>
        public async Task<T> UpdateEntityAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var id = _getIdFunc(entity);
            if (IsDefaultOrEmpty(id))
            {
                throw new ArgumentException("Cannot update entity with invalid ID", nameof(entity));
            }

            // Validate the entity
            var (isValid, errorMessage) = _crudService.ValidateEntity(entity);
            if (!isValid)
            {
                ShowError($"Validation failed: {errorMessage}");
                throw new ArgumentException(errorMessage);
            }

            try
            {
                var updatedEntity = await _crudService.UpdateAsync(entity);
                ShowSuccess("Entity updated successfully.");
                return updatedEntity;
            }
            catch (Exception ex)
            {
                ShowError($"Error updating entity: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes an entity by ID with confirmation and error handling
        /// </summary>
        public async Task DeleteEntityAsync(TKey id, string entityName = "item")
        {
            if (IsDefaultOrEmpty(id))
            {
                throw new ArgumentException("Invalid ID provided", nameof(id));
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete this {entityName}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return;

            try
            {
                await _crudService.DeleteAsync(id);
                ShowSuccess($"{entityName} deleted successfully.");
            }
            catch (Exception ex)
            {
                ShowError($"Error deleting {entityName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a paginated list of entities
        /// </summary>
        public async Task<List<T>> GetEntitiesAsync(int page, int pageSize)
        {
            try
            {
                return await _crudService.GetPagedAsync(page, pageSize);
            }
            catch (Exception ex)
            {
                ShowError($"Error loading entities: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the total count of entities
        /// </summary>
        public async Task<int> GetEntitiesCountAsync()
        {
            try
            {
                return await _crudService.GetCountAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error getting entity count: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets an entity by its ID
        /// </summary>
        public async Task<T?> GetEntityByIdAsync(TKey id)
        {
            try
            {
                return await _crudService.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                ShowError($"Error loading entity: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a value is default or empty for the given type
        /// This method properly handles different key types including Guid, int, string, etc.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to check</typeparam>
        /// <param name="value">The value to check</param>
        /// <returns>True if the value is null, default, or empty; false otherwise</returns>
        private static bool IsDefaultOrEmpty<TValue>(TValue? value)
        {
            // Handle null case
            if (value is null)
                return true;

            // Handle specific types with their own "empty" definitions
            if (value is string str)
                return string.IsNullOrWhiteSpace(str);

            if (value is Guid guid)
                return guid == Guid.Empty;

            // For value types, check if equal to default
            if (typeof(TValue).IsValueType)
                return EqualityComparer<TValue>.Default.Equals(value, default(TValue)!);

            // For reference types (other than string), only null is considered empty
            return false;
        }

        /// <summary>
        /// Shows an error message to the user
        /// </summary>
        protected virtual void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Shows a success message to the user
        /// </summary>
        protected virtual void ShowSuccess(string message)
        {
            // Optional: You can make this configurable or remove if you don't want success messages
            // MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
