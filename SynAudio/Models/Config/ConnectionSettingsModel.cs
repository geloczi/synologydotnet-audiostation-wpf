using System;
using System.Windows.Documents;

namespace SynAudio.Models.Config
{
    [Serializable]
    public class ConnectionSettingsModel
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
        /// NAS user password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Set this value if you can fetch files directly from your NAS
        /// </summary>
        public string MusicFolderPath { get; set; }

        public bool IsSet() => !(string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password));
    }
}
