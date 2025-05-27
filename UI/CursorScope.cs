// <auto-added>
#nullable enable
using System;
using System.Windows.Forms;

namespace BusBus.UI
{
    internal sealed class CursorScope : IDisposable
    {
        private readonly Cursor? _previousCursor;

        public CursorScope(Cursor cursor)
        {
            _previousCursor = Cursor.Current;
            Cursor.Current = cursor ?? Cursors.Default;
        }

        public void Dispose()
        {
            if (_previousCursor != null)
            {
                Cursor.Current = _previousCursor;
            }
        }
    }
}
