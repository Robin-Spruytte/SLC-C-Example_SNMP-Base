namespace Skyline.Protocol.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Scripting;

	public static class ProtocolExtension
	{
		public static void SetColumns(this SLProtocol protocol, Dictionary<object, List<object>> setColumnsData, DateTime dateTime = default)
		{
			// Requires Main 10.0.0 [CU?] or Feature 9.6.6 [CU?] (see RN 23815)
			int rowCount = setColumnsData.ElementAt(0).Value.Count;

			if (rowCount <= 0)
			{
				// No rows to update
				return;
			}

			object[] setColumnPids = new object[setColumnsData.Count + 1];
			object[] setColumnOptions = dateTime == default ? new object[] { true } : new object[] { true, dateTime };

			object[] setColumnValues = new object[setColumnsData.Count];

			for (int i = 0; i < setColumnValues.Length; i++)
			{
				// Sanity checks
				if (setColumnsData.ElementAt(i).Value.Count != rowCount)
				{
					protocol.Log(
						"QA" + protocol.QActionID + "|SetColumns|SetColumns on table '" + setColumnsData.Keys.ToArray()[0] + "' failed. Not all columns contain the same number of rows.",
						LogType.Error,
						LogLevel.NoLogging);

					return;
				}

				// Build set columns objects
				setColumnPids[i] = setColumnsData.ElementAt(i).Key;
				setColumnValues[i] = setColumnsData.ElementAt(i).Value.ToArray();
			}

			setColumnPids[setColumnPids.Length - 1] = setColumnOptions;
			protocol.NotifyProtocol(220, setColumnPids, setColumnValues);
		}
	}
}