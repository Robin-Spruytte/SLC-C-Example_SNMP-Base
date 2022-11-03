namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files
{
	using System;
	using System.IO;
	using Readers;

	public static class ReaderFactory
	{
		/// <summary>
		/// Gets the correct file reader.
		/// </summary>
		/// <param name="fullPath">The full file path.</param>
		/// <exception cref="ArgumentException">Path is empty.</exception>
		/// <exception cref="ArgumentNullException">Path is Null.</exception>
		/// <exception cref="FileNotFoundException">The file couldn't be found.</exception>
		/// <exception cref="NotSupportedException">The extension isn't supported.</exception>
		public static Reader<T> GetReader<T>(string fullPath) where T : class, new()
		{
			if (fullPath == null)
			{
				throw new ArgumentNullException("fullPath");
			}

			if (String.IsNullOrWhiteSpace(fullPath))
			{
				throw new ArgumentException("fullPath can't be empty.");
			}

			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException("File (" + fullPath + ") doesn't exist.");
			}

			string extension = Path.GetExtension(fullPath);

			switch (extension)
			{
				case ".csv":
					return new CsvReader<T>(fullPath);

				case ".json":
					return new JsonReader<T>(fullPath);

				case ".xml":
					return new XmlReader<T>(fullPath);

				default:
					throw new NotSupportedException("This extension (" + extension + ") isn't supported.");
			}
		}
	}
}