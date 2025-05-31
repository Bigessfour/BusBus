#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using BusBus.UI.Core;

namespace BusBus.UI
{
    /// <summary>
    /// Represents a navigation item in the side panel
    /// </summary>
    public class NavigationItem
    {
        public string Icon { get; set; }
        public string Title { get; set; }
        public string ViewName { get; set; }
        public bool IsActive { get; set; }
        public Button? Button { get; set; }

        public NavigationItem(string icon, string title, string viewName, bool isActive = false)
        {
            Icon = icon ?? throw new ArgumentNullException(nameof(icon));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            ViewName = viewName ?? throw new ArgumentNullException(nameof(viewName));
            IsActive = isActive;
        }

        /// <summary>
        /// Creates the actual button control for this navigation item
        /// </summary>
        public Button CreateButton()
        {
            var button = new Button
            {
                Text = $"{Icon} {Title}",
                Font = new Font("Segoe UI", 10.5F),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Size = new Size(220, 45),
                Margin = new Padding(10, 5, 10, 5),
                Tag = ViewName,
                Padding = new Padding(15, 0, 0, 0)
            };

            button.FlatAppearance.BorderSize = 0;
            Button = button;

            return button;
        }

        /// <summary>
        /// Updates the visual state of the button to reflect whether it's active
        /// </summary>
        public void UpdateActiveState(bool isActive)
        {
            IsActive = isActive;
            if (Button != null)
            {
                // Apply theme-appropriate styling
                if (isActive)
                {
                    Button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonHoverBackground;
                    Button.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.ButtonText;
                }
                else
                {
                    Button.BackColor = BusBus.UI.Core.ThemeManager.CurrentTheme.SidePanelBackground;
                    Button.ForeColor = BusBus.UI.Core.ThemeManager.CurrentTheme.CardText;
                }
            }
        }
    }
}
