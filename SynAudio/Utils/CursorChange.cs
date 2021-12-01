using System;
using System.Windows;
using System.Windows.Input;

namespace SynAudio.Utils
{
    /// <summary>
    /// Use this class ina using block to change the mouse cursor
    /// Example: "using (new CursorChange(Cursors.Wait)) { ... }"
    /// </summary>
    public class CursorChange : IDisposable
    {
        private Cursor _originalCursor;
        private FrameworkElement _element;

        public CursorChange(Cursor cursor)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _originalCursor = Mouse.OverrideCursor;
                Mouse.OverrideCursor = cursor;
            });
        }

        public CursorChange(FrameworkElement element, Cursor cursor)
        {
            _element = element;
            _originalCursor = _element.Cursor;
            _element.Cursor = cursor;
        }

        public void Dispose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_element is null)
                    Mouse.OverrideCursor = _originalCursor;
                else
                    _element.Cursor = _originalCursor;
            });
        }
    }
}
