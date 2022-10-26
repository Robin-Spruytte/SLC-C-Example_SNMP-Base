using System;

using Skyline.DataMiner.Scripting;

/// <summary>
/// DataMiner QAction Class: Interface Table SNMP Set.
/// </summary>
public class QAction
{
	private const int TriggerIfTable = 1000;
	private const int TriggerIfXTable = 1100;

	public static void Run(SLProtocolExt protocol)
	{
		try
		{
			int iTriggerPID = protocol.GetTriggerParameter();
			string sRowKey = protocol.RowKey();
			object oValue = protocol.GetParameter(Convert.ToInt32(iTriggerPID));

			switch (iTriggerPID)
			{
				case Parameter.Write.interfacesadminstatus:
					protocol.SetParameters(new[] { Parameter.iftablesetinstance, Parameter.Write.iftableifadminstatus }, new object[] { sRowKey, oValue });

					// Note: We poll the entire table so the bit rate calculation is triggered again using the correct values.
					// Polling just the cell or row could lead to wrong calculations.
					protocol.CheckTrigger(TriggerIfTable);
					protocol.CheckTrigger(TriggerIfXTable);
					break;

				case Parameter.Write.interfacespromiscuousmode:
					protocol.SetParameters(new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableifpromiscuousmode }, new object[] { sRowKey, oValue });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				case Parameter.Write.interfacesalias:
					protocol.SetParameters(new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableifalias }, new object[] { sRowKey, oValue });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				case Parameter.Write.interfaceslinkupdowntrapenable:
					protocol.SetParameters(new[] { Parameter.ifxtablesetinstance, Parameter.Write.ifxtableiflinkupdowntrapenable }, new object[] { sRowKey, oValue });

					protocol.CheckTrigger(TriggerIfXTable);
					break;

				default:
					// Do Nothing
					break;
			}
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|Run|Error: " + ex, LogType.Error, LogLevel.NoLogging);
		}
	}
}