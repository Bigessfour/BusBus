using System;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Event args for passing an entity in generic data grid events.
    /// </summary>
    public class EntityEventArgs<T> : EventArgs
    {
        public T Entity { get; }
        public EntityEventArgs(T entity) => Entity = entity;
    }
}
