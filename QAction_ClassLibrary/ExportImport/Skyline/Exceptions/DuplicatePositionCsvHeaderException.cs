namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes;

	[Serializable]
	public class DuplicatePositionCsvHeaderException : DuplicateCsvHeaderException
	{
		public DuplicatePositionCsvHeaderException()
		{
		}

		public DuplicatePositionCsvHeaderException(string message) : base(message)
		{
		}

		public DuplicatePositionCsvHeaderException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected DuplicatePositionCsvHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public new static DuplicatePositionCsvHeaderException From(CsvHeaderAttribute attr, Type @class)
		{
			string message = String.Format("Duplicate position '{0}' in class '{1}'.", attr.Header, @class.Name);
			return new DuplicatePositionCsvHeaderException(message);
		}
	}
}