using System;
using SynologyDotNet.AudioStation;

namespace SynAudio.Models.Config
{
    [Serializable]
    public class SettingsModel
    {
        public ConnectionSettingsModel Connection { get; set; } = new ConnectionSettingsModel();
        public TranscodeMode Transcoding { get; set; } = TranscodeMode.WAV;
        public int Volume { get; set; } = 100;
        public System.Windows.WindowState WindowState { get; set; } = System.Windows.WindowState.Maximized;
        public RectangleD WindowDimensions { get; set; }
        public RectangleD LastVirtualScreenDimensions { get; set; }
        public bool UpdateLibraryOnStartup { get; set; } = true;
    }
}
