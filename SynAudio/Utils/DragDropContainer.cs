namespace Utils
{
    public class DragDropContainer
    {
        public object Source { get; set; }
        public object DataContext { get; set; }
        public object Data { get; set; }

        public DragDropContainer(object source, object dataContext, object data)
        {
            Source = source;
            DataContext = dataContext;
            Data = data;
        }
    }
}
