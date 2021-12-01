using Newtonsoft.Json;

namespace SynAudio.DAL
{
    public class SongCustomization
    {
        /// <summary>
        /// Peak amplitude of the song samples. Used to normalize the playback volume. This is NOT equal to loudness equalization which requires complex implementation to prevent clipping. 
        /// Volume normalization gong to not cause clipping, because the sample values will be scaled up to fill the [-1.0f; +1.0f] value interval.
        /// </summary>
        public float peak { get; set; }

        /// <summary>
        /// Backup of the original Comment tag (if it was set)
        /// </summary>
        public string comment { get; set; }

        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
        public string Serialize() => JsonConvert.SerializeObject(this, SerializerSettings);
        public static SongCustomization Deserialize(string json) => JsonConvert.DeserializeObject<SongCustomization>(json);
        public static bool TryDeserialize(string json, out SongCustomization songCustomization)
        {
            songCustomization = null;
            if (string.IsNullOrEmpty(json) || json.Length < 2 || !json.StartsWith("{") || !json.EndsWith("}"))
                return false;
            try
            {
                songCustomization = Deserialize(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
