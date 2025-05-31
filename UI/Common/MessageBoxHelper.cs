using System;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Provides a way to suppress MessageBox dialogs during testing
    /// </summary>
    public static class MessageBoxHelper
    {
        /// <summary>
        /// Controls whether to show message boxes or return default values
        /// </summary>
        public static bool SuppressAllDialogs { get; set; }

        /// <summary>
        /// Shows a message box or returns a default value if suppressed
        /// </summary>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (SuppressAllDialogs)
            {
                Console.WriteLine($"[SUPPRESSED DIALOG] {caption}: {text}");

                // Return appropriate default based on buttons
                return buttons switch
                {
                    MessageBoxButtons.YesNo => DialogResult.Yes,
                    MessageBoxButtons.YesNoCancel => DialogResult.Yes,
                    MessageBoxButtons.RetryCancel => DialogResult.Retry,
                    MessageBoxButtons.AbortRetryIgnore => DialogResult.Ignore,
                    MessageBoxButtons.OKCancel => DialogResult.OK,
                    _ => DialogResult.OK
                };
            }

            // Normal behavior when not suppressing
            return MessageBox.Show(text, caption, buttons, icon);
        }

        /// <summary>
        /// Shows a message box or returns a default value if suppressed
        /// </summary>
        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        /// <summary>
        /// Shows a message box or returns a default value if suppressed
        /// </summary>
        public static DialogResult Show(string text)
        {
            return Show(text, "", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
