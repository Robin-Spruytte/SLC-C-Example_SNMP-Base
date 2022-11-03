namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Writers
{
	using System.Collections.Generic;
	using Newtonsoft.Json;

	[Skyline.DataMiner.Library.Common.Attributes.DllImport("Newtonsoft.Json.dll")]
	public class JsonWriter<T> : Writer<T> where T : class, new()
	{
		public JsonWriter(string fullPath) : base(fullPath)
		{
		}

		public override void Write(List<T> data)
		{
			string text = JsonConvert.SerializeObject(data);

			SetFileData(text);
		}
	}
}