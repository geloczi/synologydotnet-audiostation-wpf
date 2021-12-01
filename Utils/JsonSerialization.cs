using System.IO;
using Newtonsoft.Json;

namespace SynCommon.Serialization
{
	/// <summary>
	/// Serialize/Deserialize objects using JSON format, including the type names.
	/// On deserialaization, the correct object type will be returned.
	/// When deserializing, the assembly need not to match the assembly used on serialization.
	/// </summary>
	public static class JsonSerialization
	{
		private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
		{
			Formatting = Formatting.None,
			MissingMemberHandling = MissingMemberHandling.Ignore,
			NullValueHandling = NullValueHandling.Ignore,
			TypeNameHandling = TypeNameHandling.All,
			TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
		};

		private static readonly JsonSerializer _serializer = new JsonSerializer()
		{
			Formatting = _settings.Formatting,
			MissingMemberHandling = _settings.MissingMemberHandling,
			NullValueHandling = _settings.NullValueHandling,
			TypeNameHandling = _settings.TypeNameHandling,
			TypeNameAssemblyFormatHandling = _settings.TypeNameAssemblyFormatHandling
		};

		public static string SerializeToString(object o)
		{
			return JsonConvert.SerializeObject(o, _settings);
		}

		public static object DeserializeFromString(string json)
		{
			return JsonConvert.DeserializeObject(json, _settings);
		}

		public static void SerializeToStream(object o, Stream stream)
		{
			using (var sw = new StreamWriter(stream))
			using (var jtw = new JsonTextWriter(sw))
			{
				_serializer.Serialize(jtw, o);
			}
		}

		public static object DeserializeFromStream(Stream stream)
		{
			using (var sr = new StreamReader(stream))
			using (var jtr = new JsonTextReader(sr))
			{
				return _serializer.Deserialize(jtr);
			}
		}

		public static byte[] SerializeToBytes(object o)
		{
			using (var ms = new MemoryStream())
			{
				SerializeToStream(o, ms);
				return ms.ToArray();
			}
		}

		public static object DeserializeFromBytes(byte[] buffer) 
			=> DeserializeFromBytes(buffer, 0, buffer.Length);

		public static object DeserializeFromBytes(byte[] buffer, int offset, int count)
		{
			using (var ms = new MemoryStream(buffer, offset, count, false))
			{
				return DeserializeFromStream(ms);
			}
		}
	}
}
