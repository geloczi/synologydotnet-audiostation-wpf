using SynAudio.DAL;

namespace SynAudio.Models
{
    public class FolderContentsModel
    {
        public SongModel[] Songs { get; set; }
        public string[] Folders { get; set; }
    }
}
