using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SynAudio.DAL;
using SynAudio.Models.Config;
using SynAudio.Utils;
using SynAudio.ViewModels;
using Utils;
using Utils.Commands;

namespace SynAudio
{
    public partial class MainWindow : Window
    {
        private static readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private readonly WindowStateWatcher _stateWatcher;
        private readonly bool _userTriggeredWindowStateReset;

        private H.Hooks.LowLevelKeyboardHook GlobalKeyboardHook { get; }
        private MainWindowViewModel VM { get; }
        public SettingsModel Settings => App.Settings;
        public ErrorDialogErrorHandler ErrorHandler { get; } = new ErrorDialogErrorHandler();

        public MainWindow()
        {
            InitializeComponent();
            RefreshMaximizeRestoreButton();

            // Restore window dimensions only, if the screen setup is the same as last time
            // Example: The user changed the screen configuration or replaced/unplugged a screen, etc.
            _userTriggeredWindowStateReset = Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt);
            if (!_userTriggeredWindowStateReset && Settings.WindowDimensions != default && Settings.LastVirtualScreenDimensions == VirtualScreenToRectangleD())
            {
                Left = Settings.WindowDimensions.X;
                Top = Settings.WindowDimensions.Y;
                Width = Settings.WindowDimensions.Width;
                Height = Settings.WindowDimensions.Height;
            }
            else
            {
                Settings.WindowDimensions = new RectangleD(Left, Top, Width, Height);
            }

            DataContext = VM = new MainWindowViewModel(tabs1);
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.BrowseLibraryItem, VM.BrowseLibraryItemCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.PlayNow, VM.PlayNowCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.BrowseByArtists, VM.BrowseByArtistsCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.BrowseByFolders, VM.BrowseByFoldersCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.NowPlaying_ChangeCurrentSong, VM.NowPlaying_ChangeCurrentSongCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.OpenContainingFolder, VM.OpenContainingFolderCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.DeleteSelectedSongsFromLibrary, VM.DeleteSelectedSongsFromLibraryCommand));
            CommandBindings.Add(new ForwardCommandBinding(StaticCommands.CopyToClipboard, VM.CopyToClipboardCommand));

            CommandBindings.Add(new ForwardCommandBinding(MediaCommands.Play, VM.Player_PlayCommand));
            CommandBindings.Add(new ForwardCommandBinding(MediaCommands.Stop, VM.Player_StopCommand));
            CommandBindings.Add(new ForwardCommandBinding(MediaCommands.Pause, VM.Player_PauseCommand));
            CommandBindings.Add(new ForwardCommandBinding(MediaCommands.PreviousTrack, VM.Player_PlayPrevCommand));
            CommandBindings.Add(new ForwardCommandBinding(MediaCommands.NextTrack, VM.Player_PlayNextCommand));

            _stateWatcher = new WindowStateWatcher(this);
            Loaded += Window_Loaded;
            Closing += Window_Closing;
            PreviewKeyDown += Window_PreviewKeyDown;
            StateChanged += MainWindow_StateChanged;
            SizeChanged += MainWindow_SizeChanged;

            GlobalKeyboardHook = new H.Hooks.LowLevelKeyboardHook();
            GlobalKeyboardHook.Down += GlobalKeyboardHook_Down;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                Settings.WindowDimensions = new RectangleD(Left, Top, Width, Height);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            RefreshMaximizeRestoreButton();
        }

        private void RefreshMaximizeRestoreButton()
        {
            if (WindowState == WindowState.Maximized)
            {
                MaximizeButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
                mainBorder.Padding = new Thickness(7);
            }
            else
            {
                MaximizeButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
                mainBorder.Padding = new Thickness(0);
            }
        }

        private void GlobalKeyboardHook_Down(object sender, H.Hooks.KeyboardEventArgs e)
        {
            try
            {
                foreach (var key in e.Keys.Values)
                {
                    switch (key)
                    {
                        case H.Hooks.Key.MediaPlayPause:
                            e.IsHandled = true;
                            if (VM.Player_PlayCommand.CanExecute(null))
                                VM.Player_PlayCommand.Execute(null);
                            else if (VM.Player_PauseCommand.CanExecute(null))
                                VM.Player_PauseCommand.Execute(null);
                            break;

                        case H.Hooks.Key.MediaStop:
                            e.IsHandled = true;
                            if (VM.Player_StopCommand.CanExecute(null))
                                VM.Player_StopCommand.Execute(null);
                            break;

                        case H.Hooks.Key.MediaPreviousTrack:
                            e.IsHandled = true;
                            if (VM.Player_PlayPrevCommand.CanExecute(null))
                                VM.Player_PlayPrevCommand.Execute(null);
                            break;

                        case H.Hooks.Key.MediaNextTrack:
                            e.IsHandled = true;
                            if (VM.Player_PlayNextCommand.CanExecute(null))
                                VM.Player_PlayNextCommand.Execute(null);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Window_Loaded;
            UpdateWindowTitle(null);

            if (!_userTriggeredWindowStateReset)
                WindowState = Settings.WindowState == WindowState.Minimized ? WindowState.Normal : Settings.WindowState;
            
            try
            {
                await VM.Open();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(ex);
            }

            VM.PlaybackStarted += VM_PlaybackStarted;
            VM.PlaybackStopped += VM_PlaybackStopped;
            VM.NowPlaying.CurrentSongChanged += NowPlaying_CurrentSongChanged;
            VM.PropertyChanged += VM_PropertyChanged;
            GlobalKeyboardHook.Start();

            //#if DEBUG
            //            // Log focused element to help debug UI behaviour
            //            void DebugWriteLineFocusedElement()
            //            {
            //                var focused = FocusManager.GetFocusedElement(Application.Current.MainWindow);
            //                System.Diagnostics.Debug.WriteLine($"FocusedElement: {focused?.GetType().FullName}");
            //            }
            //            PreviewKeyUp += (sender, e) => DebugWriteLineFocusedElement();
            //            PreviewMouseUp += (sender, e) => DebugWriteLineFocusedElement();
            //#endif
        }

        private void VM_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(VM.CurrentTabVM):
                    CurrentTabVM_Changed();
                    break;
            }
        }

        private void CurrentTabVM_Changed()
        {
            // Toggle NowPlaying panel visibility
            if (VM.CurrentTabVM?.CurrentNavigationItem?.Action == ActionType.NowPlaying)
                VM.IsNowPlayingVisible = false;
            else
                VM.IsNowPlayingVisible = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Closing -= Window_Closing;

                GlobalKeyboardHook.Down -= GlobalKeyboardHook_Down;
                GlobalKeyboardHook.Stop();
                GlobalKeyboardHook.Dispose();

                PreviewKeyDown -= Window_PreviewKeyDown;
                VM.NowPlaying.CurrentSongChanged -= NowPlaying_CurrentSongChanged;
                VM.OnClose();

                // Save settings
                Settings.WindowState = _stateWatcher.LastVisibleState;
                Settings.LastVirtualScreenDimensions = VirtualScreenToRectangleD();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        private void VM_PlaybackStarted(MainWindowViewModel sender)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateWindowTitle(sender.NowPlaying.CurrentSong);
            }));
        }

        private void VM_PlaybackStopped(MainWindowViewModel sender)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateWindowTitle(null);
            }));
        }

        private void NowPlaying_CurrentSongChanged(NowPlayingViewModel sender)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateWindowTitle(sender.CurrentSong);
                if (!(sender.CurrentSong is null))
                    nowPlayingGrid.ScrollSongIntoView(sender.CurrentSongVM.Song.Id);
            }));
        }

        private void UpdateWindowTitle(SongModel song)
        {
            if (song is null)
                Title = MainWindowViewModel.OriginalWindowTitle;
            else
                Title = $"{song.Title} - {song.Artist}";
        }

        private static RectangleD VirtualScreenToRectangleD() => new RectangleD(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                switch (e.Key)
                {
                    case Key.Pause: //Play/Pause
                        if (VM.Player_PlayCommand.CanExecute(null))
                            VM.Player_PlayCommand.Execute(null);
                        else if (VM.Player_PauseCommand.CanExecute(null))
                            VM.Player_PauseCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.S: //Stop
                        VM.Player_StopCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.P: //Play/Pause
                        if (VM.Player_PlayCommand.CanExecute(null))
                            VM.Player_PlayCommand.Execute(null);
                        else if (VM.Player_PauseCommand.CanExecute(null))
                            VM.Player_PauseCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.D0: //Set rating
                        VM.SetRating(0).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.D1: //Set rating
                        VM.SetRating(1).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.D2: //Set rating
                        VM.SetRating(2).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.D3: //Set rating
                        VM.SetRating(3).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.D4: //Set rating
                        VM.SetRating(4).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.D5: //Set rating
                        VM.SetRating(5).FireAndForgetSafe(ErrorHandler);
                        e.Handled = true;
                        break;

                    case Key.Right: //Seek right
                        if (!(VM.NowPlaying.CurrentSong is null) && VM.Player.Length != TimeSpan.Zero)
                        {
                            var pos = VM.Player.Position.Add(TimeSpan.FromSeconds(2));
                            if (pos < VM.Player.Length)
                                VM.Player.Position = pos;
                        }
                        e.Handled = true;
                        break;

                    case Key.Left: //Seek left
                        if (!(VM.NowPlaying.CurrentSong is null) && VM.Player.Length != TimeSpan.Zero)
                        {
                            var pos = VM.Player.Position.Subtract(TimeSpan.FromSeconds(2));
                            if (pos < TimeSpan.Zero)
                                pos = TimeSpan.Zero;
                            if (pos >= TimeSpan.Zero)
                                VM.Player.Position = pos;
                        }
                        e.Handled = true;
                        break;

                    case Key.Home: //Seek to zero
                        if (!(VM.NowPlaying.CurrentSong is null) && VM.Player.Length != TimeSpan.Zero)
                            VM.Player.Position = TimeSpan.Zero;
                        e.Handled = true;
                        break;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Key.Right: //Seek right (fast)
                        if (!(VM.NowPlaying.CurrentSong is null) && VM.Player.Length != TimeSpan.Zero)
                        {
                            var pos = VM.Player.Position.Add(TimeSpan.FromSeconds(10));
                            if (pos < VM.Player.Length)
                                VM.Player.Position = pos;
                        }
                        e.Handled = true;
                        break;

                    case Key.Left: //Seek left (fast)
                        if (!(VM.NowPlaying.CurrentSong is null) && VM.Player.Length != TimeSpan.Zero)
                        {
                            var pos = VM.Player.Position.Subtract(TimeSpan.FromSeconds(10));
                            if (pos < TimeSpan.Zero)
                                pos = TimeSpan.Zero;
                            if (pos >= TimeSpan.Zero)
                                VM.Player.Position = pos;
                        }
                        e.Handled = true;
                        break;
                }
            }
        }

        private void RatingControl_ValueChanged(object sender, EventArgs e)
        {
            if (sender is Controls.RatingControl rc)
                VM.SetRating(rc.Value).FireAndForgetSafe(ErrorHandler);
        }

        private void tabs1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (VM.CurrentTabVM?.NavigationItems.Count > 2)
                    VM.CurrentTabVM.NavigateCommand.Execute(VM.CurrentTabVM.NavigationItems[VM.CurrentTabVM.NavigationItems.Count - 3]);
                e.Handled = true;
            }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

    }
}
