using System;
using System.Collections.Generic;
using System.Linq;
using Skyline.DataMiner.Scripting;
using SLNetMessages = Skyline.DataMiner.Net.Messages;

/// <summary>
/// DataMiner QAction Class: Merge Interface Tables.
/// </summary>
public class QAction
{
	private const uint MaxReportableIfSpeed = UInt32.MaxValue;

	// RFC 2863: For interfaces that operate at 20,000,000 (20 million) bits per 	second or less, 32-bit byte and packet counters MUST be supported.
	// For interfaces that operate faster than 20,000,000 bits/second, and 	slower than 650,000,000 bits/second, 32-bit packet counters MUST be
	// supported and 64-bit octet counters MUST be supported.For interfaces that operate at 650,000,000 bits/second or faster, 64-bit packet counters AND 64-bit octet counters MUST be supported.
	// We choose to use 64-bit counters if the speed is higher than 20Mbps as this will result in fewer wraparounds.
	private const double SpeedLimitForCounters = 20000000;

	/// <summary>
	/// The QAction entry point.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol process.</param>
	public static void Run(SLProtocolExt protocol)
	{
		try
		{
			IfTable iftable = new IfTable(protocol);
			IfXTable ifxtable = new IfXTable(protocol);

			Dictionary<string, InterfacesQActionRow> iterfaceTableRowsMap = new Dictionary<string, InterfacesQActionRow>();

			Dictionary<string, int> duplexStatusValues = GetDuplexStatus(protocol);

			for (int i = 0; i < iftable.Keys.Length; i++)
			{
				InterfacesQActionRow interfaceTableRow = new InterfacesQActionRow();
				MergeFromSnmpItfTable(interfaceTableRow, iftable, i);

				string key = Convert.ToString(iftable.Keys[i]);
				int duplexState;
				if (duplexStatusValues.TryGetValue(key, out duplexState))
				{
					interfaceTableRow.Interfacesduplexstatus = duplexState;
				}
				else
				{
					interfaceTableRow.Interfacesduplexstatus = -1; // N/A
				}

				iterfaceTableRowsMap.Add(Convert.ToString(iftable.Keys[i]), interfaceTableRow);
			}

			// Add data from ifXTable.
			for (int i = 0; i < ifxtable.IfIndex.Length; i++)
			{
				string index = Convert.ToString(ifxtable.IfIndex[i]);

				InterfacesQActionRow interfaceTableRow;

				if (iterfaceTableRowsMap.TryGetValue(index, out interfaceTableRow))
				{
					MergeFromSnmpItfxTable(interfaceTableRow, ifxtable, i);
				}
			}

			var rows = iterfaceTableRowsMap.Values.ToArray();
			protocol.interfaces.FillArray(rows);
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|Run|Error: " + ex, LogType.Error, LogLevel.NoLogging);
		}
	}

	private static Dictionary<string, int> GetDuplexStatus(SLProtocol protocol)
	{
		object[] columns = (object[])protocol.NotifyProtocol((int)SLNetMessages.NotifyType.NT_GET_TABLE_COLUMNS, Parameter.Dot3statstable.tablePid, new uint[] { Parameter.Dot3statstable.Idx.dot3statsindex_1301, Parameter.Dot3statstable.Idx.dot3statsduplexstatus_1302 });

		Dictionary<string, int> duplexStatus = new Dictionary<string, int>();

		if (columns.Length == 2 && ((object[])columns[0]).Length == ((object[])columns[1]).Length)
		{
			object[] pkeyColumn = (object[])columns[0];
			object[] duplexStateColumn = (object[])columns[1];

			for (int i = 0; i < pkeyColumn.Length; i++)
			{
				duplexStatus[Convert.ToString(pkeyColumn[i])] = Convert.ToInt32(duplexStateColumn[i]);
			}
		}

		return duplexStatus;
	}

	private static void MergeFromSnmpItfTable(InterfacesQActionRow interfaceTableRow, IfTable iftable, int i)
	{
		interfaceTableRow.Interfacesindex = Convert.ToString(iftable.Keys[i]);
		interfaceTableRow.Interfacesdescr = Convert.ToString(iftable.IfDescriptions[i]);
		interfaceTableRow.Interfacestype = Convert.ToDouble(iftable.IfTypes[i]);
		interfaceTableRow.Interfacesmtu = Convert.ToDouble(iftable.IfMtus[i]);
		interfaceTableRow.Interfacesphysaddress = Convert.ToString(iftable.IfPhysAddress[i]);
		interfaceTableRow.Interfacesadminstatus = Convert.ToDouble(iftable.IfAdminStatus[i]);
		interfaceTableRow.Interfacesoperstatus = Convert.ToDouble(iftable.IfOperStatus[i]);
		interfaceTableRow.Interfaceslastchange = Convert.ToDouble(iftable.IfLastChange[i]);

		interfaceTableRow.Interfacesindiscards = Convert.ToDouble(iftable.IfInDiscards[i]);
		interfaceTableRow.Interfacesinerrors = Convert.ToDouble(iftable.IfInErrors[i]);
		interfaceTableRow.Interfacesinunknownprotos = Convert.ToDouble(iftable.IfInUnknownProtos[i]);

		interfaceTableRow.Interfacesoutdiscards = Convert.ToDouble(iftable.IfOutDiscards[i]);
		interfaceTableRow.Interfacesouterrors = Convert.ToDouble(iftable.IfOutErrors[i]);

		if (Convert.ToUInt32(iftable.IfSpeeds[i]) != MaxReportableIfSpeed)
		{
			// Speed in ifTable is expressed in bps, whereas speed in Interface table is expressed in Mbps.
			interfaceTableRow.Interfacesspeed = Convert.ToDouble(iftable.IfSpeeds[i]) / 1000000;
		}

		if (Convert.ToDouble(iftable.IfSpeeds[i]) <= SpeedLimitForCounters)
		{
			// This means we should use the 32-bit versions.
			interfaceTableRow.Interfacesinoctets = Convert.ToDouble(iftable.IfInOctets[i]);
			interfaceTableRow.Interfacesinucastpkts = Convert.ToDouble(iftable.IfInUcastpkts[i]);
			interfaceTableRow.Interfacesoutoctets = Convert.ToDouble(iftable.IfOutOctets[i]);
			interfaceTableRow.Interfacesoutucastpkts = Convert.ToDouble(iftable.IfOutUcastpkts[i]);

			interfaceTableRow.Interfacesbandwidthutilization = Convert.ToDouble(iftable.IfBandwidthUtilization[i]);

			double dBitRateIn = Convert.ToDouble(iftable.IfBitRateIn[i]);
			double dBitRateOut = Convert.ToDouble(iftable.IfBitRateOut[i]);
			if (dBitRateIn >= 0)
			{
				interfaceTableRow.Interfacesinbitrate = dBitRateIn / 1000000;
			}
			else
			{
				interfaceTableRow.Interfacesinbitrate = -1;
			}

			if (dBitRateOut >= 0)
			{
				interfaceTableRow.Interfacesoutbitrate = dBitRateOut / 1000000;
			}
			else
			{
				interfaceTableRow.Interfacesoutbitrate = -1;
			}
		}
	}

	private static void MergeFromSnmpItfxTable(InterfacesQActionRow interfaceTableRow, IfXTable ifxtable, int i)
	{
		interfaceTableRow.Interfacespromiscuousmode = ifxtable.IfPromiscuousMode[i] == null ? -1 : Convert.ToDouble(ifxtable.IfPromiscuousMode[i]);
		interfaceTableRow.Interfacesphysicalconnector = ifxtable.IfConnectorPresent[i] == null ? -1 : Convert.ToDouble(ifxtable.IfConnectorPresent[i]);
		interfaceTableRow.Interfacesalias = Convert.ToString(ifxtable.IfAlias[i]);
		interfaceTableRow.Interfacescounterdiscontinuitytime = Convert.ToDouble(ifxtable.IfCounterDiscontinuitytime[i]) / 100;
		interfaceTableRow.Interfaceslinkupdowntrapenable = Convert.ToDouble(ifxtable.IfLinkUpDownTrapEnable[i]);

		if (interfaceTableRow.Interfacesspeed == null)
		{
			interfaceTableRow.Interfacesspeed = ifxtable.IfHighSpeed[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHighSpeed[i]);
		}

		if (interfaceTableRow.Interfacesinoctets == null)
		{
			Use64BitCounters(interfaceTableRow, ifxtable, i);
		}
		else
		{
			Use32BitCounters(interfaceTableRow, ifxtable, i);
		}
	}

	private static void Use32BitCounters(InterfacesQActionRow interfaceTableRow, IfXTable ifxtable, int i)
	{
		interfaceTableRow.Interfacesinmulticastpkts = ifxtable.IfInMulticastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfInMulticastPkts[i]);
		interfaceTableRow.Interfacesinbroadcastpkts = ifxtable.IfInBroadcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfInBroadcastPkts[i]);

		interfaceTableRow.Interfacesoutmulticastpkts = ifxtable.IfOutMulticastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfOutMulticastPkts[i]);
		interfaceTableRow.Interfacesoutbroadcastpkts = ifxtable.IfOutBroadcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfOutBroadcastPkts[i]);

		double dBitrateIn = Convert.ToDouble(ifxtable.IfBitRateIn[i]);
		if (dBitrateIn < -1)
		{
			// indication of discontinuity times, need to set values to N/A
			interfaceTableRow.Interfacesinbitrate = -1;
			interfaceTableRow.Interfacesoutbitrate = -1;
			interfaceTableRow.Interfacesbandwidthutilization = -1;
		}
	}

	private static void Use64BitCounters(InterfacesQActionRow interfaceTableRow, IfXTable ifxtable, int i)
	{
		interfaceTableRow.Interfacesinoctets = ifxtable.IfHcInOctets[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcInOctets[i]);
		interfaceTableRow.Interfacesinucastpkts = ifxtable.IfHcInUcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcInUcastPkts[i]);
		interfaceTableRow.Interfacesinmulticastpkts = ifxtable.IfHcInMulticastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcInMulticastPkts[i]);
		interfaceTableRow.Interfacesinbroadcastpkts = ifxtable.IfHcInBroadcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcInBroadcastPkts[i]);

		interfaceTableRow.Interfacesoutoctets = ifxtable.IfHcOutOctets[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcOutOctets[i]);
		interfaceTableRow.Interfacesoutucastpkts = ifxtable.IfHcOutUcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcOutUcastPkts[i]);
		interfaceTableRow.Interfacesoutmulticastpkts = ifxtable.IfHcOutMulticastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcOutMulticastPkts[i]);
		interfaceTableRow.Interfacesoutbroadcastpkts = ifxtable.IfHcOutBroadcastPkts[i] == null ? -1 : Convert.ToDouble(ifxtable.IfHcOutBroadcastPkts[i]);

		interfaceTableRow.Interfacesbandwidthutilization = Convert.ToDouble(ifxtable.IfBandwidthUtilization[i]);

		double bitrateIn = Convert.ToDouble(ifxtable.IfBitRateIn[i]);
		double bitrateOut = Convert.ToDouble(ifxtable.IfBitRateOut[i]);

		if (bitrateIn >= 0)
		{
			interfaceTableRow.Interfacesinbitrate = bitrateIn / 1000000;
		}
		else
		{
			interfaceTableRow.Interfacesinbitrate = -1;
		}

		if (bitrateOut >= 0)
		{
			interfaceTableRow.Interfacesoutbitrate = bitrateOut / 1000000;
		}
		else
		{
			interfaceTableRow.Interfacesoutbitrate = -1;
		}
	}
}

public class IfTable
{
	public IfTable(SLProtocol protocol)
	{
		uint[] uiIfTableIdx = new uint[]
		{
			Parameter.Iftable.Idx.iftableifindex,
			Parameter.Iftable.Idx.iftableifdescr,
			Parameter.Iftable.Idx.iftableiftype,
			Parameter.Iftable.Idx.iftableifmtu,
			Parameter.Iftable.Idx.iftableifspeed,
			Parameter.Iftable.Idx.iftableifphysaddress,
			Parameter.Iftable.Idx.iftableifadminstatus,
			Parameter.Iftable.Idx.iftableifoperstatus,
			Parameter.Iftable.Idx.iftableiflastchange,
			Parameter.Iftable.Idx.iftableifinoctets,
			Parameter.Iftable.Idx.iftableifinucastpkts,
			Parameter.Iftable.Idx.iftableifindiscards,
			Parameter.Iftable.Idx.iftableifinerrors,
			Parameter.Iftable.Idx.iftableifinunknownprotos,
			Parameter.Iftable.Idx.iftableifoutoctets,
			Parameter.Iftable.Idx.iftableifoutucastpkts,
			Parameter.Iftable.Idx.iftableifoutdiscards,
			Parameter.Iftable.Idx.iftableifouterrors,
			Parameter.Iftable.Idx.iftableifinbitrate,
			Parameter.Iftable.Idx.iftableifoutbitrate,
			Parameter.Iftable.Idx.iftableifbandwidthutilization,
		};

		object[] interfaceTable = (object[])protocol.NotifyProtocol((int)SLNetMessages.NotifyType.NT_GET_TABLE_COLUMNS, Parameter.Iftable.tablePid, uiIfTableIdx);

		this.Keys = (object[])interfaceTable[0];
		this.IfDescriptions = (object[])interfaceTable[1];
		this.IfTypes = (object[])interfaceTable[2];
		this.IfMtus = (object[])interfaceTable[3];
		this.IfSpeeds = (object[])interfaceTable[4];
		this.IfPhysAddress = (object[])interfaceTable[5];
		this.IfAdminStatus = (object[])interfaceTable[6];
		this.IfOperStatus = (object[])interfaceTable[7];
		this.IfLastChange = (object[])interfaceTable[8];
		this.IfInOctets = (object[])interfaceTable[9];
		this.IfInUcastpkts = (object[])interfaceTable[10];
		this.IfInDiscards = (object[])interfaceTable[11];
		this.IfInErrors = (object[])interfaceTable[12];
		this.IfInUnknownProtos = (object[])interfaceTable[13];
		this.IfOutOctets = (object[])interfaceTable[14];
		this.IfOutUcastpkts = (object[])interfaceTable[15];
		this.IfOutDiscards = (object[])interfaceTable[16];
		this.IfOutErrors = (object[])interfaceTable[17];
		this.IfBitRateIn = (object[])interfaceTable[18];
		this.IfBitRateOut = (object[])interfaceTable[19];
		this.IfBandwidthUtilization = (object[])interfaceTable[20];
	}

	public object[] IfAdminStatus { get; set; }

	public object[] IfBandwidthUtilization { get; set; }

	public object[] IfBitRateIn { get; set; }

	public object[] IfBitRateOut { get; set; }

	public object[] IfDescriptions { get; set; }

	public object[] IfInDiscards { get; set; }

	public object[] IfInErrors { get; set; }

	public object[] IfInOctets { get; set; }

	public object[] IfInUcastpkts { get; set; }

	public object[] IfInUnknownProtos { get; set; }

	public object[] IfLastChange { get; set; }

	public object[] IfMtus { get; set; }

	public object[] IfOperStatus { get; set; }

	public object[] IfOutDiscards { get; set; }

	public object[] IfOutErrors { get; set; }

	public object[] IfOutOctets { get; set; }

	public object[] IfOutUcastpkts { get; set; }

	public object[] IfPhysAddress { get; set; }

	public object[] IfSpeeds { get; set; }

	public object[] IfTypes { get; set; }

	public object[] Keys { get; set; }
}

public class IfXTable
{
	public IfXTable(SLProtocol protocol)
	{
		uint[] uiIfXTableIdx = new uint[]
		{
			Parameter.Ifxtable.Idx.ifxtableifindex,
			Parameter.Ifxtable.Idx.ifxtableifname,
			Parameter.Ifxtable.Idx.ifxtableifinmulticastpkts,
			Parameter.Ifxtable.Idx.ifxtableifinbroadcastpkts,
			Parameter.Ifxtable.Idx.ifxtableifoutmulticastpkts,
			Parameter.Ifxtable.Idx.ifxtableifoutbroadcastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcinoctets,
			Parameter.Ifxtable.Idx.ifxtableifhcinucastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcinmulticastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcinbroadcastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcoutoctets,
			Parameter.Ifxtable.Idx.ifxtableifhcoutucastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcoutmulticastpkts,
			Parameter.Ifxtable.Idx.ifxtableifhcoutbroadcastpkts,
			Parameter.Ifxtable.Idx.ifxtableiflinkupdowntrapenable,
			Parameter.Ifxtable.Idx.ifxtableifhighspeed,
			Parameter.Ifxtable.Idx.ifxtableifpromiscuousmode,
			Parameter.Ifxtable.Idx.ifxtableifconnectorpresent,
			Parameter.Ifxtable.Idx.ifxtableifalias,
			Parameter.Ifxtable.Idx.ifxtableifcounterdiscontinuitytime,
			Parameter.Ifxtable.Idx.ifxtableifinbitrate,
			Parameter.Ifxtable.Idx.ifxtableifoutbitrate,
			Parameter.Ifxtable.Idx.ifxtableifbandwidthutilization,
		};

		object[] aoIfXTable = (object[])protocol.NotifyProtocol((int)SLNetMessages.NotifyType.NT_GET_TABLE_COLUMNS, Parameter.Ifxtable.tablePid, uiIfXTableIdx);

		this.IfIndex = (object[])aoIfXTable[0];
		this.IfName = (object[])aoIfXTable[1];
		this.IfInMulticastPkts = (object[])aoIfXTable[2];
		this.IfInBroadcastPkts = (object[])aoIfXTable[3];
		this.IfOutMulticastPkts = (object[])aoIfXTable[4];
		this.IfOutBroadcastPkts = (object[])aoIfXTable[5];
		this.IfHcInOctets = (object[])aoIfXTable[6];
		this.IfHcInUcastPkts = (object[])aoIfXTable[7];
		this.IfHcInMulticastPkts = (object[])aoIfXTable[8];
		this.IfHcInBroadcastPkts = (object[])aoIfXTable[9];
		this.IfHcOutOctets = (object[])aoIfXTable[10];
		this.IfHcOutUcastPkts = (object[])aoIfXTable[11];
		this.IfHcOutMulticastPkts = (object[])aoIfXTable[12];
		this.IfHcOutBroadcastPkts = (object[])aoIfXTable[13];
		this.IfLinkUpDownTrapEnable = (object[])aoIfXTable[14];
		this.IfHighSpeed = (object[])aoIfXTable[15];
		this.IfPromiscuousMode = (object[])aoIfXTable[16];
		this.IfConnectorPresent = (object[])aoIfXTable[17];
		this.IfAlias = (object[])aoIfXTable[18];
		this.IfCounterDiscontinuitytime = (object[])aoIfXTable[19];
		this.IfBitRateIn = (object[])aoIfXTable[20];
		this.IfBitRateOut = (object[])aoIfXTable[21];
		this.IfBandwidthUtilization = (object[])aoIfXTable[22];
	}

	public object[] IfAlias { get; set; }

	public object[] IfBandwidthUtilization { get; set; }

	public object[] IfBitRateIn { get; set; }

	public object[] IfBitRateOut { get; set; }

	public object[] IfConnectorPresent { get; set; }

	public object[] IfCounterDiscontinuitytime { get; set; }

	public object[] IfHcInBroadcastPkts { get; set; }

	public object[] IfHcInMulticastPkts { get; set; }

	public object[] IfHcInOctets { get; set; }

	public object[] IfHcInUcastPkts { get; set; }

	public object[] IfHcOutBroadcastPkts { get; set; }

	public object[] IfHcOutMulticastPkts { get; set; }

	public object[] IfHcOutOctets { get; set; }

	public object[] IfHcOutUcastPkts { get; set; }

	public object[] IfHighSpeed { get; set; }

	public object[] IfInBroadcastPkts { get; set; }

	public object[] IfIndex { get; set; }

	public object[] IfInMulticastPkts { get; set; }

	public object[] IfLinkUpDownTrapEnable { get; set; }

	public object[] IfName { get; set; }

	public object[] IfOutBroadcastPkts { get; set; }

	public object[] IfOutMulticastPkts { get; set; }

	public object[] IfPromiscuousMode { get; set; }
}