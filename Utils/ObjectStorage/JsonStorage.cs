using Newtonsoft.Json;
using System.IO;

namespace Utils.ObjectStorage
{
    public class JsonStorage : IObjectStorage
    {
        public string Folder { get; }
        public JsonSerializerSettings Settings { get; }
        public JsonStorage(string folder, JsonSerializerSettings settings)
        {
            Folder = folder;
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
            Settings = settings;
        }

        public void Save(string name, object o) => File.WriteAllText(GetFilePath(name), JsonConvert.SerializeObject(o, Settings));

        public T Load<T>(string name) => JsonConvert.DeserializeObject<T>(File.ReadAllText(GetFilePath(name)), Settings);

        public bool TryLoad<T>(string name, out T o)
        {
            try
            {
                if (Exists(name))
                {
                    o = JsonConvert.DeserializeObject<T>(File.ReadAllText(GetFilePath(name)), Settings);
                    return true;
                }
            }
            catch { }
            o = default;
            return false;
        }

        public bool Exists(string name) => File.Exists(GetFilePath(name));

        private string GetFilePath(string name) => Path.Combine(Folder, name + ".json");
    }
}
