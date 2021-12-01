using SynAudio.Models;

namespace SynAudio.ViewModels
{
    public class LoginDialogViewModel : ViewModelBase
    {
        public string Url { get; set; }
        public string MusicFolder { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
