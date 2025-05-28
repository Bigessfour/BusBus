#nullable enable
using System;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Event args for entity operations
    /// </summary>
    public class EntityEventArgs<T> : EventArgs
    {
        public T Entity { get; }

        public EntityEventArgs(T entity)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        }
    }
}
