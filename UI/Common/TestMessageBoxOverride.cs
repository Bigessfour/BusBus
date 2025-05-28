using System;
using System.Windows.Forms;

namespace BusBus.UI.Common
{
    /// <summary>
    /// Static helper class that intercepts all MessageBox calls during testing
    /// </summary>
    public static class TestMessageBoxOverride
    {
        public static bool SuppressAllDialogs { get; set; } = false;

        /// <summary>
        /// Intercepts MessageBox.Show calls during tests
        /// </summary>
        public static DialogResult ShowDialog(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
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
        /// Overload for simpler MessageBox calls
        /// </summary>
        public static DialogResult ShowDialog(string text, string caption)
        {
            return ShowDialog(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        /// <summary>
        /// Overload for simpler MessageBox calls
        /// </summary>
        public static DialogResult ShowDialog(string text)
        {
            return ShowDialog(text, "", MessageBoxButtons.OK, MessageBoxIcon.None);
        }
    }
}
