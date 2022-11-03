namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes;

	[Serializable]
	public class DuplicateCsvHeaderException : Exception
	{
		public DuplicateCsvHeaderException()
		{
		}

		public DuplicateCsvHeaderException(string message) : base(message)
		{
		}

		public DuplicateCsvHeaderException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected DuplicateCsvHeaderException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public static DuplicateCsvHeaderException From(CsvHeaderAttribute attr, Type @class)
		{
			string message = String.Format("Duplicate attribute values in class '{0}'", @class.Name);
			return new DuplicateCsvHeaderException(message);
		}
	}
}