using Newtonsoft.Json;
using System.IO;

namespace Utils.ObjectStorage
{
    public class JsonStorage : IObjectStorage
    {
        public string Folder { get; }
        public JsonSerializerSettings SerializerSettings { get; set; } = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented
        };

        public JsonStorage(string folder)
        {
            Folder = folder;
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);
        }

        public void Save(string name, object o) => File.WriteAllText(GetFilePath(name), JsonConvert.SerializeObject(o, SerializerSettings));

        public T Load<T>(string name) => JsonConvert.DeserializeObject<T>(File.ReadAllText(GetFilePath(name)), SerializerSettings);

        public bool TryLoad<T>(string name, out T o)
        {
            try
            {
                if (Exists(name))
                {
                    o = JsonConvert.DeserializeObject<T>(File.ReadAllText(GetFilePath(name)), SerializerSettings);
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
