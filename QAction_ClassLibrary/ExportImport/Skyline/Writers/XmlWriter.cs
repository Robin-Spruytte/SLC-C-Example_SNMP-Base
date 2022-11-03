namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Writers
{
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Xml.Serialization;

	[Skyline.DataMiner.Library.Common.Attributes.DllImport("System.Xml.dll")]
	public class XmlWriter<T> : Writer<T> where T : class, new()
	{
		public XmlWriter(string fullPath) : base(fullPath)
		{
		}

		public override void Write(List<T> data)
		{
			using (StreamWriter sw = new StreamWriter(new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), Encoding.Default))
			{
				new XmlSerializer(typeof(List<T>)).Serialize(sw, data);
			}
		}
	}
}