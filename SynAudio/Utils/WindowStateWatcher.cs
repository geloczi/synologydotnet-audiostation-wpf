using System;
using System.Windows;

namespace SynAudio.Utils
{
    public class WindowStateWatcher
    {
        public WindowState LastVisibleState { get; private set; } = WindowState.Normal;
        public bool IsMinimized { get; private set; }

        public WindowStateWatcher(Window window)
        {
            W_StateChanged(window, null);
            window.StateChanged += W_StateChanged;
            window.Closed += Window_Closed;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            ((Window)sender).Closed -= Window_Closed;
            ((Window)sender).StateChanged -= W_StateChanged;
        }

        private void W_StateChanged(object sender, EventArgs e)
        {
            var state = ((Window)sender).WindowState;
            switch (state)
            {
                case WindowState.Maximized:
                case WindowState.Normal:
                    IsMinimized = false;
                    LastVisibleState = state;
                    break;

                case WindowState.Minimized:
                    IsMinimized = true;
                    break;
            }
        }
    }
}
