namespace SynAudio.ViewModels
{
    public class TabContentViewModel : ViewModelBase
    {
        public object Content { get; set; }
        public ActionType Action { get; set; }
        public object SelectedItem { get; set; }
    }
}
