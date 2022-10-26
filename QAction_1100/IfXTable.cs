namespace Skyline.Protocol.IfxTable
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Library.Common.Rates;
	using Skyline.DataMiner.Library.Common.SafeConverters;
	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;
	using Skyline.DataMiner.Scripting;
	using Skyline.Protocol.Extensions;

	public class IfxRateData
	{
		public SnmpRate64 BitrateInData { get; set; }

		public SnmpRate64 BitrateOutData { get; set; }

		public string PreviousDiscontinuity { get; set; }

		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}

		internal static IfxRateData FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new IfxRateData
				{
					BitrateInData = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOutData = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					PreviousDiscontinuity = String.Empty,
				};
			}

			return JsonConvert.DeserializeObject<IfxRateData>(serializedIfxRateData);
		}
	}

	public class IfxTableTimeoutProcessor
	{
		private const int GroupId = 1100;
		private static readonly TimeSpan MinDelta = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan MaxDelta = new TimeSpan(0, 10, 0);

		private readonly SLProtocol protocol;

		private readonly IfxTableGetter ifxtableGetter;
		private readonly IfxTableSetter ifxtableSetter;

		public IfxTableTimeoutProcessor(SLProtocol protocol)
		{
			this.protocol = protocol;

			ifxtableGetter = new IfxTableGetter(protocol);
			ifxtableGetter.Load();

			ifxtableSetter = new IfxTableSetter(protocol);
		}

		public void ProcessTimeout()
		{
			SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId, Parameter.interfacesratecalculationsmethod);

			for (int i = 0; i < ifxtableGetter.Keys.Length; i++)
			{
				string streamPK = Convert.ToString(ifxtableGetter.Keys[i]);
				string serializedIfxRateData = Convert.ToString(ifxtableGetter.IfRateData[i]);

				IfxRateData rateData = IfxRateData.FromJsonString(serializedIfxRateData, MinDelta, MaxDelta);

				rateData.BitrateInData.BufferDelta(snmpDeltaHelper, streamPK);
				rateData.BitrateOutData.BufferDelta(snmpDeltaHelper, streamPK);

				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.tablePid].Add(streamPK);
				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifratedata].Add(rateData.ToJsonString());
			}
		}

		public void UpdateProtocol()
		{
			ifxtableSetter.SetColumns();
		}

		private class IfxTableGetter
		{
			private readonly SLProtocol protocol;

			public IfxTableGetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public object[] Keys { get; private set; }

			public object[] IfRateData { get; private set; }

			public void Load()
			{
				var tableData = (object[])protocol.NotifyProtocol(321, Parameter.Ifxtable.tablePid, new uint[]
				{
					Parameter.Ifxtable.Idx.ifxtableifindex,
					Parameter.Ifxtable.Idx.ifxtableifratedata,
				});

				Keys = (object[])tableData[0];
				IfRateData = (object[])tableData[1];
			}
		}

		private class IfxTableSetter
		{
			private readonly SLProtocol protocol;

			public IfxTableSetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public Dictionary<object, List<object>> SetColumnsData { get; } = new Dictionary<object, List<object>>
			{
				{ Parameter.Ifxtable.tablePid, new List<object>() },
				{ Parameter.Ifxtable.Pid.ifxtableifratedata, new List<object>() },
			};

			public void SetColumns()
			{
				protocol.SetColumns(SetColumnsData);
			}
		}
	}

	public class IfxTableProcessor
	{
		private const int GroupId = 1100;
		private static readonly TimeSpan MinDelta = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan MaxDelta = new TimeSpan(0, 10, 0);

		private readonly SLProtocol protocol;

		private readonly IfxTableGetter ifxtableGetter;
		private readonly IfxTableSetter ifxtableSetter;
		private readonly DuplexGetter duplexGetter;

		public IfxTableProcessor(SLProtocol protocol)
		{
			this.protocol = protocol;

			ifxtableGetter = new IfxTableGetter(protocol);
			ifxtableGetter.Load();
			duplexGetter = new DuplexGetter(protocol);
			duplexGetter.Load();

			CopyDiscontinuityToIfTable(protocol);

			ifxtableSetter = new IfxTableSetter(protocol);
		}

		public static double CalculateUtilization(double inputRate, double outputRate, double interfaceSpeed, DuplexStatus duplexStatus)
		{
			if (inputRate < 0 || outputRate < 0 || interfaceSpeed <= 0)
			{
				return -1;
			}

			if (duplexStatus == DuplexStatus.HalfDuplex)
			{
				return (inputRate + outputRate) * 100 / interfaceSpeed;
			}

			if (duplexStatus == DuplexStatus.FullDuplex)
			{
				return Math.Max(inputRate, outputRate) * 100 / interfaceSpeed;
			}

			return -1;
		}

		public void CopyDiscontinuityToIfTable(SLProtocol protocol)
		{
			protocol.NotifyProtocol(
				220 /*NT_FILL_ARRAY_WITH_COLUMN*/,
				new object[] { Parameter.Iftable.tablePid, Parameter.Iftable.Pid.iftableifcounterdiscontinuitytime },
				new object[] { ifxtableGetter.Keys, ifxtableGetter.Discontinuity });
		}

		public void ProcessData()
		{
			SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId, Parameter.interfacesratecalculationsmethod);

			Dictionary<string, DuplexStatus> duplexStatuses = ConvertDuplexColumnToDictionary();

			for (int i = 0; i < ifxtableGetter.Keys.Length; i++)
			{
				string serializedIfxRateData = Convert.ToString(ifxtableGetter.IfxRateData[i]);
				string key = Convert.ToString(ifxtableGetter.Keys[i]);
				uint speedInTable = Convert.ToUInt32(ifxtableGetter.Speed[i]);

				double speedValueToUse = Convert.ToDouble(speedInTable) * 1000000;

				DuplexStatus duplexStatus = duplexStatuses.ContainsKey(key)
					? duplexStatuses[key]
					: DuplexStatus.NotInitialized;

				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.tablePid].Add(Convert.ToString(ifxtableGetter.Keys[i]));

				IfxRateData rateData = IfxRateData.FromJsonString(serializedIfxRateData, MinDelta, MaxDelta);
				bool discontinuity = CheckDiscontinuity(i, rateData.PreviousDiscontinuity);

				ifxtableSetter.SetParamsData[Parameter.ifxtablesnmpagentrestartflag] = 0;
				if (ifxtableGetter.IsSnmpAgentRestarted || discontinuity)
				{
					rateData.BitrateInData = SnmpRate64.FromJsonString(String.Empty, MinDelta, MaxDelta);
					rateData.BitrateOutData = SnmpRate64.FromJsonString(String.Empty, MinDelta, MaxDelta);
				}

				double bitrateIn = CalculateBitrateIn(i, snmpDeltaHelper, rateData.BitrateInData);
				double bitrateOut = CalculateBitrateOut(i, snmpDeltaHelper, rateData.BitrateOutData);
				double utilization = CalculateUtilization(bitrateIn, bitrateOut, speedValueToUse, duplexStatus);

				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifinbitrate].Add(bitrateIn);
				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifoutbitrate].Add(bitrateOut);
				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifbandwidthutilization].Add(utilization);
				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifratedata].Add(rateData.ToJsonString());
			}
		}

		public void UpdateProtocol()
		{
			ifxtableSetter.SetColumns();
			ifxtableSetter.SetParams();
		}

		private bool CheckDiscontinuity(int i, string previousDiscontinuity)
		{
			string currentDiscontinuity = Convert.ToString(ifxtableGetter.Discontinuity[i]);

			if (!String.IsNullOrEmpty(previousDiscontinuity) && currentDiscontinuity != previousDiscontinuity)
			{
				return true;
			}

			return false;
		}

		private Dictionary<string, DuplexStatus> ConvertDuplexColumnToDictionary()
		{
			Dictionary<string, DuplexStatus> duplexStatuses = new Dictionary<string, DuplexStatus>();
			for (int i = 0; i < duplexGetter.Keys.Length; i++)
			{
				string key = Convert.ToString(duplexGetter.Keys[i]);
				DuplexStatus duplexStatus = (DuplexStatus)Convert.ToInt32(duplexGetter.DuplexStatuses[i]);
				duplexStatuses[key] = duplexStatus;
			}

			return duplexStatuses;
		}

		private double CalculateBitrateIn(int getPosition, SnmpDeltaHelper snmpDeltaHelper, SnmpRate64 snmpRateHelper)
		{
			string streamPK = Convert.ToString(ifxtableGetter.Keys[getPosition]);
			ulong octetsIn = SafeConvert.ToUInt64(Convert.ToDouble(ifxtableGetter.OctetsIn[getPosition]));

			double octetRateIn = snmpRateHelper.Calculate(snmpDeltaHelper, octetsIn, streamPK);
			double bitRateIn = octetRateIn > 0 ? octetRateIn / 8 : octetRateIn;

			return bitRateIn;
		}

		private double CalculateBitrateOut(int getPosition, SnmpDeltaHelper snmpDeltaHelper, SnmpRate64 snmpRateHelper)
		{
			string streamPK = Convert.ToString(ifxtableGetter.Keys[getPosition]);
			ulong octetsOut = SafeConvert.ToUInt64(Convert.ToDouble(ifxtableGetter.OctetsOut[getPosition]));
			double octetRateOut = snmpRateHelper.Calculate(snmpDeltaHelper, octetsOut, streamPK);
			double bitRateOut = octetRateOut > 0 ? octetRateOut / 8 : octetRateOut;

			return bitRateOut;
		}

		private class DuplexGetter
		{
			private readonly SLProtocol protocol;

			public DuplexGetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public object[] Keys { get; private set; }

			public object[] DuplexStatuses { get; private set; }

			public void Load()
			{
				var tableData = (object[])protocol.NotifyProtocol(321, Parameter.Dot3statstable.tablePid, new uint[]
				{
					Parameter.Dot3statstable.Idx.dot3statsindex,
					Parameter.Dot3statstable.Idx.dot3statsduplexstatus,
				});

				Keys = (object[])tableData[0];
				DuplexStatuses = (object[])tableData[1];
			}
		}

		private class IfxTableGetter
		{
			private readonly SLProtocol protocol;

			public IfxTableGetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public object[] Keys { get; private set; }

			public object[] OctetsIn { get; private set; }

			public object[] OctetsOut { get; private set; }

			public object[] Utilization { get; private set; }

			public object[] Speed { get; private set; }

			public object[] Discontinuity { get; private set; }

			public object[] IfxRateData { get; private set; }

			public bool IsSnmpAgentRestarted { get; private set; }

			public void Load()
			{
				IsSnmpAgentRestarted = Convert.ToBoolean(Convert.ToInt16(protocol.GetParameter(Parameter.ifxtabletimeoutafterretriesflag)));

				List<uint> columnsToGet = new List<uint>
				{
					Parameter.Ifxtable.Idx.ifxtableifindex,
					Parameter.Ifxtable.Idx.ifxtableifhcinoctets,
					Parameter.Ifxtable.Idx.ifxtableifhcoutoctets,
					Parameter.Ifxtable.Idx.ifxtableifbandwidthutilization,
					Parameter.Ifxtable.Idx.ifxtableifhighspeed,
					Parameter.Ifxtable.Idx.ifxtableifcounterdiscontinuitytime,
				};

				if (!IsSnmpAgentRestarted)
				{
					columnsToGet.Add(Parameter.Ifxtable.Idx.ifxtableifratedata);
				}

				var tableData = (object[])protocol.NotifyProtocol(321, Parameter.Ifxtable.tablePid, columnsToGet.ToArray());

				Keys = (object[])tableData[0];
				OctetsIn = (object[])tableData[1];
				OctetsOut = (object[])tableData[2];
				Utilization = (object[])tableData[3];
				Speed = (object[])tableData[4];
				Discontinuity = (object[])tableData[5];

				if (!IsSnmpAgentRestarted)
				{
					IfxRateData = (object[])tableData[6];
				}
			}
		}

		private class IfxTableSetter
		{
			private readonly SLProtocol protocol;

			public IfxTableSetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public Dictionary<object, List<object>> SetColumnsData { get; } = new Dictionary<object, List<object>>
			{
				{ Parameter.Ifxtable.tablePid, new List<object>() },
				{ Parameter.Ifxtable.Pid.ifxtableifinbitrate, new List<object>() },
				{ Parameter.Ifxtable.Pid.ifxtableifoutbitrate, new List<object>() },
				{ Parameter.Ifxtable.Pid.ifxtableifbandwidthutilization, new List<object>() },
				{ Parameter.Ifxtable.Pid.ifxtableifratedata, new List<object>() },
			};

			internal Dictionary<int, object> SetParamsData { get; } = new Dictionary<int, object>();

			public void SetColumns()
			{
				protocol.SetColumns(SetColumnsData);
			}

			public void SetParams()
			{
				protocol.SetParameters(SetParamsData.Keys.ToArray(), SetParamsData.Values.ToArray());
			}
		}
	}
}
