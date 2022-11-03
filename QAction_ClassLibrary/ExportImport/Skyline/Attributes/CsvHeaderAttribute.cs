namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes
{
	using System;

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class CsvHeaderAttribute : Attribute
	{
		public CsvHeaderAttribute(string headerName)
		{
			Header = headerName;
			Position = UInt16.MaxValue;
		}

		public CsvHeaderAttribute(ushort index)
		{
			Position = index;
		}

		public string Header { get; private set; }

		public ushort Position { get; private set; }
	}
}