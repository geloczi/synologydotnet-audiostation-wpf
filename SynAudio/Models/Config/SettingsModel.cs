using System;
using SynologyDotNet.AudioStation;

namespace SynAudio.Models.Config
{
    [Serializable]
    public class SettingsModel
    {
        /// <summary>
        /// Url to the NAS, like "https://yourserver.example.com:5001"
        /// Both HTTP and HTTPS endpoints are supported.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// NAS user account name
        /// </summary>
        public string Username { get; set; }

        public TranscodeMode Transcoding { get; set; } = TranscodeMode.WAV;
        public int Volume { get; set; } = 100;
        public System.Windows.WindowState WindowState { get; set; } = System.Windows.WindowState.Maximized;
        public RectangleD WindowDimensions { get; set; }
        public RectangleD LastVirtualScreenDimensions { get; set; }
        public bool UpdateLibraryOnStartup { get; set; } = true;
        public bool RestrictedMode { get; set; } = true;
    }
}
