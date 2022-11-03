namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Readers
{
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	public abstract class Reader<T> where T : class, new()
	{
		private readonly string fullPath;

		protected Reader(string fullPath)
		{
			this.fullPath = fullPath;
		}

		/// <summary>
		/// Reads this file.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidDataException">Couldn't parse/convert certain values inside the file.</exception>
		public abstract List<T> Read();

		protected IEnumerable<string> GetFileData()
		{
			if (!File.Exists(fullPath))
			{
				yield break;
			}

			// Make sure it can be opened even if its used by another process
			// Read the file with encoding => used for chars like 'é'
			using (StreamReader sr = new StreamReader(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.Default))
			{
				while (!sr.EndOfStream)
				{
					yield return sr.ReadLine();
				}
			}
		}
	}
}