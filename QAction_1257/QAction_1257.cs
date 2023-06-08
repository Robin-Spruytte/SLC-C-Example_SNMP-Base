using System;

using Skyline.DataMiner.Scripting;

/// <summary>
/// DataMiner QAction Class: Interface Table SNMP Sets.
/// </summary>
public class QAction
{
	private const int TriggerIfTable = 1000;
	private const int TriggerIfXTable = 1100;
	private const int TriggerInterfaceMerge = 1191;

	/// <summary>
	/// The QAction entry point.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol process.</param>
	public static void Run(SLProtocol protocol)
	{
		try
		{
			int triggerPid = protocol.GetTriggerParameter();
			string rowKey = protocol.RowKey();
			object value = protocol.GetParameter(Convert.ToInt32(triggerPid));

			switch (triggerPid)
			{
				case Parameter.Write.interfacesadminstatus:
					protocol.SetParameters(
						new[] { Parameter.iftablesetinstance, Parameter.Write.iftableifadminstatus },
						new[] { rowKey, value });

					// Note: We poll the entire table so the bit rate calculation is triggered again using the correct values.
					// Polling just the cell or row could lead to wrong calculations.
					protocol.CheckTrigger(TriggerIfTable);
					break;

				case Parameter.Write.interfacespromiscuousmode:
					protocol.SetParameters(
						new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableifpromiscuousmode },
						new[] { rowKey, value });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				case Parameter.Write.interfacesalias:
					protocol.SetParameters(
						new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableifalias },
						new[] { rowKey, value });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				case Parameter.Write.interfaceslinkupdowntrapenable:
					protocol.SetParameters(
						new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableiflinkupdowntrapenable },
						new[] { rowKey, value });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				default:
					protocol.Log(
						"QA" + protocol.QActionID + "|Run|QAction triggered by unexpected param '" + triggerPid + "'",
						LogType.Error,
						LogLevel.NoLogging);
					break;
			}

			protocol.CheckTrigger(TriggerInterfaceMerge);
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|Run|Error: " + ex, LogType.Error, LogLevel.NoLogging);
		}
	}
}