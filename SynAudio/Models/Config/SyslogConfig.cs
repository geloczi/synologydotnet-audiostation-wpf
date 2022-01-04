namespace SynAudio.Models.Config
{
    /// <summary>
    /// Syslog configuration. 
    /// On you Synology, open Log Center, navigate to "Log Receiving", and create an entry with IETF format using UDP protocol. 
    /// </summary>
    public class SyslogConfig
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string Level { get; set; }
    }
}
