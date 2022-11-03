namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Readers
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json;

	[Skyline.DataMiner.Library.Common.Attributes.DllImport("Newtonsoft.Json.dll")]
	public class JsonReader<T> : Reader<T> where T : class, new()
	{
		public JsonReader(string fullPath) : base(fullPath)
		{
		}

		public override List<T> Read()
		{
			string text = String.Join(Environment.NewLine, GetFileData());

			return JsonConvert.DeserializeObject<List<T>>(text);
		}
	}
}