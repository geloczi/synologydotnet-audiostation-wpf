using System.Collections.Generic;
using System.Windows.Input;
using SynAudio.Utils;
using SynAudio.Models.Config;
using SynAudio.ViewModels;
using System;
using MusicPlayback;

namespace SynAudio.Views
{
    public class SettingsDialogModel : ViewModelBase
    {
        public static SettingsModel Settings => App.Settings;

        public MainWindowViewModel Main { get; set; }
        
        public List<HotkeyViewModel> Hotkeys { get; } = new List<HotkeyViewModel>();

        public RangeObservableCollection<object> OutputDeviceItems { get; } = new RangeObservableCollection<object>();

        public string ConnectionState => Main.Connected ? "Connected" : "Disconnected";

        public Styles.Theme[] ThemeItems { get; } = Enum.GetValues<Styles.Theme>();

        public OutputApiType[] OutputApiItems { get; } = Enum.GetValues<OutputApiType>();

        public SettingsDialogModel(MainWindowViewModel main)
        {
            Main = main;
            Hotkeys.Add(new HotkeyViewModel("Reset main window at startup", ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt));
            Hotkeys.Add(new HotkeyViewModel("Song rating: clear", ModifierKeys.Alt, Key.D0));
            Hotkeys.Add(new HotkeyViewModel("Song rating: 1 star", ModifierKeys.Alt, Key.D1));
            Hotkeys.Add(new HotkeyViewModel("Song rating: 2 stars", ModifierKeys.Alt, Key.D2));
            Hotkeys.Add(new HotkeyViewModel("Song rating: 3 stars", ModifierKeys.Alt, Key.D3));
            Hotkeys.Add(new HotkeyViewModel("Song rating: 4 stars", ModifierKeys.Alt, Key.D4));
            Hotkeys.Add(new HotkeyViewModel("Song rating: 5 stars", ModifierKeys.Alt, Key.D5));
        }
    }
}
