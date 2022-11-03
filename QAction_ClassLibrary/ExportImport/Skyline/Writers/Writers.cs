namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Writers
{
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	public abstract class Writer<T> where T : class, new()
	{
		protected readonly string fullPath;

		protected Writer(string fullPath)
		{
			this.fullPath = fullPath;
		}

		/// <summary>
		/// Writes the specified data to the file.
		/// </summary>
		/// <param name="data">The data to write.</param>
		public abstract void Write(List<T> data);

		protected void SetFileData(string text)
		{
			// Make sure it can be opened even if its used by another process
			// Write the file with encoding => used for chars like 'é'
			using (StreamWriter sw = new StreamWriter(new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite), Encoding.Default))
			{
				sw.Write(text);
			}
		}
	}
}