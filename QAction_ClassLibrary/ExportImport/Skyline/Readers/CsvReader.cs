namespace Skyline.DataMiner.DeveloperCommunityLibrary.Files.Readers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	using Skyline.DataMiner.DeveloperCommunityLibrary.Files.Attributes;
	using Skyline.DataMiner.DeveloperCommunityLibrary.Files.Exceptions;

	public class CsvReader<T> : Reader<T> where T : class, new()
	{
		private static char? separator;

		public CsvReader(string fullPath) : base(fullPath)
		{
		}

		public override List<T> Read()
		{
			return HasPositionHeaders() ? ReadViaPositions() : ReadViaHeaders();
		}

		private List<T> ReadViaPositions()
		{
			string[] lines = GetFileData().ToArray();

			// <Position, Setter>
			Dictionary<ushort, PropertyInfo> props = GetPositionProperties();

			List<T> data = new List<T>();
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];

				if (String.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				string[] columns = SplitCsvRow(line);

				if (columns.Length != props.Count)
				{
					throw InvalidColumnCountException.From(columns.Length, i, props.Count);
				}

				T dto = new T();
				for (ushort pos = 0; pos < columns.Length; pos++)
				{
					// Get HeaderName
					string name = null;
					PropertyInfo setter;
					if (!props.TryGetValue(pos, out setter))
					{
						continue;
					}

					try
					{
						Type t = Nullable.GetUnderlyingType(setter.PropertyType) ?? setter.PropertyType;

						object value;
						if (String.IsNullOrEmpty(columns[pos]))
						{
							value = null;
						}
						else
						{
							if (setter.PropertyType.IsEnum)
							{
								value = Enum.Parse(setter.PropertyType, columns[pos]);
							}
							else
							{
								value = Convert.ChangeType(columns[pos], t);
							}
						}

						setter.SetMethod.Invoke(dto, new object[] { value });
					}
					catch (Exception ex)
					{
						if (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
						{
							throw new InvalidDataException(String.Format("Failed to convert the value for header '{0}' on row {1}", name, i), ex);
						}

						throw;
					}
				}

				data.Add(dto);
			}

			return data;
		}

		private List<T> ReadViaHeaders()
		{
			string[] lines = GetFileData().ToArray();

			if (lines.Length <= 1)
			{
				return new List<T>();
			}

			string headerLine = lines[0];

			// <Position, HeaderName>
			Dictionary<int, string> positions = GetPositions(headerLine);

			// <HeaderName, Setter>
			Dictionary<string, PropertyInfo> props = GetHeaderProperties();

			List<T> data = new List<T>();
			for (int i = 1; i < lines.Length; i++)
			{
				string line = lines[i];

				if (String.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				string[] columns = SplitCsvRow(line);

				if (columns.Length != positions.Count)
				{
					throw InvalidColumnCountException.From(columns.Length, i, positions.Count);
				}

				T dto = new T();
				for (int pos = 0; pos < columns.Length; pos++)
				{
					// Get HeaderName
					string name;
					PropertyInfo setter;
					if (!positions.TryGetValue(pos, out name) || !props.TryGetValue(name, out setter))
					{
						continue;
					}

					try
					{
						Type t = Nullable.GetUnderlyingType(setter.PropertyType) ?? setter.PropertyType;

						object value;
						if (String.IsNullOrEmpty(columns[pos]))
						{
							value = null;
						}
						else
						{
							if (setter.PropertyType.IsEnum)
							{
								value = Enum.Parse(setter.PropertyType, columns[pos]);
							}
							else
							{
								value = Convert.ChangeType(columns[pos], t);
							}
						}

						setter.SetMethod.Invoke(dto, new object[] { value });
					}
					catch (Exception ex)
					{
						if (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
						{
							throw new InvalidDataException(String.Format("Failed to convert the value for header '{0}' on row {1}", name, i), ex);
						}

						throw;
					}
				}

				data.Add(dto);
			}

			return data;
		}

		private static bool HasPositionHeaders()
		{
			var tempClass = typeof(T);
			var tempProps = tempClass.GetProperties();

			foreach (var tempProp in tempProps)
			{
				var attr = tempProp.GetCustomAttribute<CsvHeaderAttribute>();

				if (attr == null)
				{
					continue;
				}

				if (attr.Position != UInt16.MaxValue)
				{
					return true;
				}
			}

			return false;
		}

		private static Dictionary<string, PropertyInfo> GetHeaderProperties()
		{
			var tempClass = typeof(T);
			var tempProps = tempClass.GetProperties();

			Dictionary<string, PropertyInfo> props = new Dictionary<string, PropertyInfo>();
			foreach (var tempProp in tempProps)
			{
				var attr = tempProp.GetCustomAttribute<CsvHeaderAttribute>();
				var ignore = tempProp.GetCustomAttribute<CsvIgnoreAttribute>();

				if (ignore != null)
				{
					continue;
				}

				var name = attr == null ? tempProp.Name : attr.Header;

				if (name == null)
				{
					continue;
				}

				if (props.ContainsKey(name))
				{
					throw DuplicateHeaderCsvHeaderException.From(attr, tempClass);
				}

				props.Add(name, tempProp);
			}

			return props;
		}

		private static Dictionary<ushort, PropertyInfo> GetPositionProperties()
		{
			var tempClass = typeof(T);
			var tempProps = tempClass.GetProperties();

			Dictionary<ushort, PropertyInfo> props = new Dictionary<ushort, PropertyInfo>();
			foreach (var tempProp in tempProps)
			{
				var attr = tempProp.GetCustomAttribute<CsvHeaderAttribute>();
				var ignore = tempProp.GetCustomAttribute<CsvIgnoreAttribute>();

				if (ignore != null)
				{
					continue;
				}

				if (attr == null || attr.Position == UInt16.MaxValue)
				{
					// Missing CsvHeaderAttribute
					// Invalid CsvHeaderAttribute defined
					throw MissingCsvHeaderAttributeWithPositionException.FromPropertyInfo(tempProp);
				}

				if (props.ContainsKey(attr.Position))
				{
					throw DuplicatePositionCsvHeaderException.From(attr, tempClass);
				}

				props.Add(attr.Position, tempProp);
			}

			return props;
		}

		private static Dictionary<int, string> GetPositions(string headerLine)
		{
			string[] temp = SplitCsvRow(headerLine);

			Dictionary<int, string> positions = new Dictionary<int, string>();

			for (int i = 0; i < temp.Length; i++)
			{
				positions.Add(i, temp[i]);
			}

			return positions;
		}

		private static string[] SplitCsvRow(string row)
		{
			// Detect the column separator
			GetUsedSeparator(row);
			return ParseLine(row);
		}

		private static void GetUsedSeparator(string row)
		{
			if (separator != null)
			{
				return;
			}

			if (row.Count(c => c == ';') > row.Count(c => c == ','))
			{
				separator = ';';
			}
			else
			{
				separator = ',';
			}
		}

		private static string[] ParseLine(string line)
		{
			StringBuilder temp = new StringBuilder();

			List<string> parts = new List<string>();
			bool _stringSection = false;
			bool _escapeQuote = false;

			bool _prevCharIsQuote = false;
			bool _realCharFound = false;
			bool _ignoreExtraQuote = false;
			int pos = 0;
			foreach (char c in line)
			{
				switch (c)
				{
					case '"':
						if (pos + 1 < line.Length)
						{
							if (line[pos + 1] == '"' /*&& _escapeQuote*/)
							{
								// See this char as a real char

								// "" is ", """" is ""
								if (!_ignoreExtraQuote)
								{
									// Check for ,"",  or "", or ,""
									bool emptyString = false;
									if (pos + 2 < line.Length)
									{
										if (line[pos + 2] == separator && (pos - 1 <= 0 || line[pos - 1] == separator))
										{
											// We have ,"",  or (start) "",
											// Empty
											emptyString = true;
										}
									}
									else
									{
										// ,"" (end)
										emptyString = true;
									}

									if (!emptyString)
									{
										temp.Append(c);
									}

									_ignoreExtraQuote = true;
								}
								else
								{
									_ignoreExtraQuote = false;
								}

								_escapeQuote = false;
								_prevCharIsQuote = false;
								_realCharFound = true;
							}
						}

						if (_escapeQuote && _realCharFound)
						{
							temp.Append(c);
						}
						else
						{
							_stringSection = !_stringSection;
						}

						_escapeQuote = !_escapeQuote;

						_prevCharIsQuote = true;
						_realCharFound = false;
						break;

					default:
						if (c == separator && !_stringSection)
						{
							parts.Add(temp.ToString());
							temp.Clear();
							if (_prevCharIsQuote)
							{
								_escapeQuote = false;
							}

							break;
						}

						temp.Append(c);

						_escapeQuote = false;
						_prevCharIsQuote = false;
						_realCharFound = true;

						_ignoreExtraQuote = false;
						break;
				}

				pos++;
			}

			parts.Add(temp.ToString());

			return parts.ToArray();
		}
	}
}