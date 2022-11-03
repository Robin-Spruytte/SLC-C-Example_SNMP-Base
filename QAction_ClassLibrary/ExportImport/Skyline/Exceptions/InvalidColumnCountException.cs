namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class InvalidColumnCountException : Exception
	{
		public InvalidColumnCountException()
		{
		}

		public InvalidColumnCountException(string message) : base(message)
		{
		}

		public InvalidColumnCountException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected InvalidColumnCountException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public static InvalidColumnCountException From(int columnCount, int rowNumber, int expectedCount)
		{
			string message = String.Format("Invalid Column Count ({0}) for row {1}. Expected Column Count: {2}", columnCount, rowNumber, expectedCount);
			return new InvalidColumnCountException(message);
		}
	}
}