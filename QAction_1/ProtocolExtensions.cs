namespace Skyline.Protocol.Extension
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Scripting;

	public static class ProtocolExtension
	{
		/// <summary>
		/// Removes the rows with the specified primary keys from the specified table.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="keysToDelete">The primary keys of the rows to remove.</param>
		/// <exception cref="ArgumentNullException"><paramref name="keysToDelete"/> is <see langword="null"/>.</exception>
		public static void DeleteRow(this SLProtocol protocol, int tablePid, IEnumerable<object> keysToDelete)
		{
			// Sanity checks
			if (keysToDelete == null)
				throw new ArgumentNullException(nameof(keysToDelete));

			var keysToDeleteArray = keysToDelete.ToArray();

			if (keysToDeleteArray.Length == 0)
			{
				// No rows to delete
				return;
			}

			// Build delete row object
			string[] deleteRowKeys = new string[keysToDeleteArray.Length];
			for (int i = 0; i < deleteRowKeys.Length; i++)
			{
				deleteRowKeys[i] = (string)keysToDeleteArray[i];
			}

			// Delete rows
			protocol.NotifyProtocol(156, tablePid, deleteRowKeys);
		}

		/// <summary>
		/// Removes the rows with the specified primary keys from the specified table.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="keysToDelete">The primary keys of the rows to remove.</param>
		/// <exception cref="ArgumentNullException"><paramref name="keysToDelete"/> is <see langword="null"/>.</exception>
		public static void DeleteRow(this SLProtocol protocol, int tablePid, IEnumerable<string> keysToDelete)
		{
			// Sanity checks
			if (keysToDelete == null)
				throw new ArgumentNullException(nameof(keysToDelete));

			var deleteRowKeys = keysToDelete.ToArray();

			if (deleteRowKeys.Length == 0)
			{
				// No rows to delete
				return;
			}

			// Delete rows
			protocol.NotifyProtocol(156, tablePid, deleteRowKeys);
		}

		/// <summary>
		/// Retrieves a cell from a table with the specified <paramref name="tablePid"/>, <paramref name="rowPK"/> and the <paramref name="columnIdx"/>.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="rowPK">The primary key of the row.</param>
		/// <param name="columnIdx">The 0-based position of the column, corresponding to the idx as defined in protocol.xml file.</param>
		/// <returns>The value of the cell.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="rowPK"/> is <see langword="null"/>.</exception>
		/// <remarks>
		/// <list type="bullet">
		/// <para>The tablePid can be retrieved with the static Parameter class.</para>
		/// <para>The columnIdx can be retrieved with the static Parameter.[table].Idx class.</para>
		/// <para>returns <see langword="null"/> for uninitialized cells.</para>
		/// </list>
		/// </remarks>
		public static object GetCell(this SLProtocol protocol, int tablePid, string rowPK, int columnIdx)
		{
			if (rowPK == null)
				throw new ArgumentNullException(nameof(rowPK));

			return protocol.NotifyProtocol(122, new object[] { tablePid, rowPK, columnIdx + 1 }, null);
		}

		/// <summary>
		/// Retrieves the values of the column with the specified <paramref name="tablePid"/> and <paramref name="columnIdx"/>.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="columnIdx">The 0-based position of the column, corresponding to the idx as defined in protocol.xml file.</param>
		/// <returns>The values of the retrieved column.</returns>
		public static object[] GetColumn(this SLProtocol protocol, int tablePid, uint columnIdx)
		{
			var columns = protocol.GetColumns(tablePid, new uint[] { columnIdx });
			return (object[])columns[0];
		}

		/// <summary>
		/// Retrieves the values of the columns with the specified <paramref name="tablePid"/> and <paramref name="columnsIdx"/>.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="columnsIdx">The 0-based positions of the columns, corresponding to the idx as defined in protocol.xml file.</param>
		/// <returns>The values of the retrieved columns.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="columnsIdx"/> is <see langword="null"/>.</exception>
		public static object[] GetColumns(this SLProtocol protocol, int tablePid, IEnumerable<uint> columnsIdx)
		{
			// Sanity checks
			if (columnsIdx == null)
				throw new ArgumentNullException(nameof(columnsIdx));

			var columnsIdxArray = columnsIdx.ToArray();

			if (columnsIdxArray.Length == 0)
			{
				return new object[] { };
			}

			return (object[])protocol.NotifyProtocol(321, tablePid, columnsIdxArray);
		}

		/// <summary>
		/// Sets the value of a cell in a table, identified by the primary key of the row and column position, with the specified value.
		/// Use <see langword="null"/> as <paramref name="value"/> to clear the cell.
		/// The <paramref name="tablePid"/> can be retrieved with the static Parameter class.
		/// The <paramref name="columnIdx"/> can be retrieved with the static Parameter.[table].Idx class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="tablePid">The ID of the table parameter.</param>
		/// <param name="rowPK">The primary key of the row.</param>
		/// <param name="columnIdx">The 0-based column position.</param>
		/// <param name="value">The new value. Use <see langword="null"/> to clear the cell.</param>
		/// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
		/// <returns>Whether the cell value has changed. <see langword="true"/> indicates change; otherwise, <see langword="false"/>.</returns>
		/// <remarks>The primary key can never be updated.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="rowPK"/> is <see langword="null"/>.</exception>
		public static bool SetCell(this SLProtocol protocol, int tablePid, string rowPK, int columnIdx, object value, DateTime? dateTime = null)
		{
			if (rowPK == null)
				throw new ArgumentNullException(nameof(rowPK));

			if (dateTime == null)
			{
				return protocol.SetParameterIndexByKey(tablePid, rowPK, columnIdx + 1, value);
			}
			else
			{
				return protocol.SetParameterIndexByKey(tablePid, rowPK, columnIdx + 1, value, dateTime.Value);
			}
		}

		/// <summary>
		/// Sets the specified columns.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="columnsPid">The column parameter ID of the columns to update. First item should contain the table PID. Primary key column PID should never be provided.</param>
		/// <param name="columnsValues">The column values for each column to update. First item should contain the primary keys as <see cref="string" />.</param>
		/// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
		/// <exception cref="ArgumentNullException"><paramref name="columnsPid"/> or <paramref name="columnsValues"/> is <see langword="null"/>.</exception>
		public static void SetColumns(this SLProtocol protocol, IList<int> columnsPid, IList<IEnumerable<object>> columnsValues, DateTime? dateTime = null)
		{
			// Sanity checks
			if (columnsPid == null)
				throw new ArgumentNullException(nameof(columnsPid));

			if (columnsValues == null)
				throw new ArgumentNullException(nameof(columnsValues));

			if (columnsPid.Count != columnsValues.Count)
				throw new ArgumentException($"Length of {nameof(columnsPid)} '{columnsPid.Count}' != length of {nameof(columnsValues)} '{columnsValues.Count}'.");

			// Prepare data
			int columnsCount = columnsPid.Count;

			object[] columnsPidArray = new object[columnsCount + 1];
			object[] columnsValuesArray = new object[columnsCount];

			for (int i = 0; i < columnsCount; i++)
			{
				columnsPidArray[i] = columnsPid[i];
				columnsValuesArray[i] = columnsValues[i].ToArray();
			}

			// Options (Clear & Leave, history sets)
			object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
			columnsPidArray[columnsCount] = setColumnOptions;

			// Set columns
			protocol.NotifyProtocol(220, columnsPidArray, columnsValuesArray);
		}

		/// <summary>
		/// Sets the specified columns (Requires Main 10.0.0 [CU?] or Feature 9.6.6 [CU?] (see RN 23815)).
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="setColumnsData">The new column values per column PID. The first dictionary item should contain table PID as key and primary keys as value.</param>
		/// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
		/// <exception cref="ArgumentNullException"><paramref name="setColumnsData"/> is <see langword="null"/>.</exception>
		public static void SetColumns(this SLProtocol protocol, IDictionary<int, List<object>> setColumnsData, DateTime? dateTime = null)
		{
			// Sanity checks
			if (setColumnsData == null)
				throw new ArgumentNullException(nameof(setColumnsData));

			if (setColumnsData.Count == 0)
				return;

			int rowCount = setColumnsData.ElementAt(0).Value.Count;
			if (rowCount == 0)
			{
				// No rows to update
				return;
			}

			// Prepare data
			object[] setColumnPids = new object[setColumnsData.Count + 1];
			object[] setColumnValues = new object[setColumnsData.Count];

			int columnPos = 0;
			foreach (var setColumnData in setColumnsData)
			{
				// Sanity checks
				if (setColumnData.Value.Count != rowCount)
				{
					protocol.Log(
						$"QA{protocol.QActionID}|SetColumns|SetColumns on table '{setColumnsData.Keys.ToArray()[0]}' failed. " +
							$"Not all columns contain the same number of rows.",
						LogType.Error,
						LogLevel.NoLogging);

					return;
				}

				// Build set columns objects
				setColumnPids[columnPos] = setColumnData.Key;
				setColumnValues[columnPos] = setColumnData.Value.ToArray();

				columnPos++;
			}

			// Options (Clear & Leave, history sets)
			object[] setColumnOptions = dateTime == null ? new object[] { true } : new object[] { true, dateTime.Value };
			setColumnPids[setColumnPids.Length - 1] = setColumnOptions;

			// Set columns
			protocol.NotifyProtocol(220, setColumnPids, setColumnValues);
		}

		/// <summary>
		/// Sets the specified parameters to the specified values.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol process.</param>
		/// <param name="paramsToSet">The IDs of the parameters to set with their value to set.</param>
		/// <param name="dateTime">The time stamp for the new values (in case of historySets).</param>
		/// <exception cref="ArgumentNullException"><paramref name="paramsToSet"/> is <see langword="null"/>.</exception>
		public static void SetParams(this SLProtocol protocol, IDictionary<int, object> paramsToSet, DateTime? dateTime = null)
		{
			// Sanity checks
			if (paramsToSet == null)
				throw new ArgumentNullException(nameof(paramsToSet));

			if (paramsToSet.Count == 0)
				return;

			if (dateTime == null)
			{
				protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray());
			}
			else
			{
				DateTime[] historySetDates = new DateTime[paramsToSet.Count];
				for (int i = 0; i < historySetDates.Length; i++)
				{
					historySetDates[i] = dateTime.Value;
				}

				protocol.SetParameters(paramsToSet.Keys.ToArray(), paramsToSet.Values.ToArray(), historySetDates);
			}
		}
	}
}