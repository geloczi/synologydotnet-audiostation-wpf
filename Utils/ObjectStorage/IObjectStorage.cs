namespace Utils.ObjectStorage
{
    public interface IObjectStorage
    {
        bool Exists(string name);
        void Save(string name, object o);
        T Load<T>(string name);
        bool TryLoad<T>(string name, out T o);
    }
}
