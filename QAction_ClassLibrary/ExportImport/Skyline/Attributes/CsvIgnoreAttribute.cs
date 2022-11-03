namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes
{
	using System;

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class CsvIgnoreAttribute : Attribute
	{
		public CsvIgnoreAttribute()
		{
		}
	}
}