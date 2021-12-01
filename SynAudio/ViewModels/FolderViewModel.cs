namespace SynAudio.ViewModels
{
    public class FolderViewModel : ViewModelBase
    {
        public string Path { get; set; }
        public string Name { get; }

        public FolderViewModel() { }
        public FolderViewModel(string path)
        {
            Path = path;
            Name = path.Remove(0, Path.LastIndexOf('/') + 1);
        }

        public override string ToString() => Name;
    }
}
