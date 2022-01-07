using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        /// <summary>
        /// NAS user password (encrypted)
        /// </summary>
        public string Password { get; set; }

        public bool SavePassword { get; set; }


        [JsonConverter(typeof(StringEnumConverter))]
        public TranscodeMode Transcoding { get; set; } = TranscodeMode.WAV;

        public int Volume { get; set; } = 100;

        [JsonConverter(typeof(StringEnumConverter))]
        public System.Windows.WindowState WindowState { get; set; } = System.Windows.WindowState.Maximized;

        public RectangleD WindowDimensions { get; set; }

        public RectangleD LastVirtualScreenDimensions { get; set; }

        public bool UpdateLibraryOnStartup { get; set; } = true;

        public bool SimpleMode { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Styles.Theme Theme { get; set; } = Styles.Theme.Dark;

        public SyslogConfig LogToSyslog { get; set; }

        public bool TryDecryptSavedPassword(out string password)
        {
            password = null;
            if (!string.IsNullOrEmpty(Password))
            {
                try
                {
                    password = App.Encrypter.Decrypt(Password);
                    return true;
                }
                catch { }
            }
            return false;
        }

        public string GetMd5Hash() => Utils.Md5Hash.FromObject(this);
    }
}
