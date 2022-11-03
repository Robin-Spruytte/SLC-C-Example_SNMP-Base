namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes;

	[Serializable]
	public class DuplicateHeaderCsvHeaderException : DuplicateCsvHeaderException
	{
		public DuplicateHeaderCsvHeaderException()
		{
		}

		public DuplicateHeaderCsvHeaderException(string message) : base(message)
		{
		}

		public DuplicateHeaderCsvHeaderException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected DuplicateHeaderCsvHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public new static DuplicateHeaderCsvHeaderException From(CsvHeaderAttribute attr, Type @class)
		{
			string message = String.Format("Duplicate header '{0}' in class '{1}'.", attr.Header, @class.Name);
			return new DuplicateHeaderCsvHeaderException(message);
		}
	}
}