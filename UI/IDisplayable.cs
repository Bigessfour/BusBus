using System;
using System.Windows.Forms;

namespace BusBus.UI
{
    /// <summary>
    /// Represents a displayable view or control for the Dashboard.
    /// </summary>
    public interface IDisplayable : IDisposable
    {
        /// <summary>
        /// Renders the displayable content into the specified container control.
        /// </summary>
        /// <param name="container">The container control to render into.</param>
        void Render(Control container);

        /// <summary>
        /// Refreshes the theme of the displayable control and its children.
        /// </summary>
        void RefreshTheme();
    }
}