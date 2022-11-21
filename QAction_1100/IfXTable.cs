namespace Skyline.Protocol.IfxTable
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Library.Common.Rates;
	using Skyline.DataMiner.Library.Common.SafeConverters;
	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;
	using Skyline.DataMiner.Scripting;
	using Skyline.Protocol.Extensions;

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
				string key = Convert.ToString(ifxtableGetter.Keys[i]);
				string serializedIfxRateData = Convert.ToString(ifxtableGetter.IfRateData[i]);

				InterfaceRateData64 rateData = InterfaceRateData64.FromJsonString(serializedIfxRateData, MinDelta, MaxDelta);

				rateData.BitrateInData.BufferDelta(snmpDeltaHelper, key);
				rateData.BitrateOutData.BufferDelta(snmpDeltaHelper, key);

				ifxtableSetter.SetColumnsData[Parameter.Ifxtable.tablePid].Add(key);
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
				uint[] columnsToGet = new uint[]
				{
					Parameter.Ifxtable.Idx.ifxtableifindex,
					Parameter.Ifxtable.Idx.ifxtableifratedata,
				};

				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Ifxtable.tablePid, columnsToGet);

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

		private readonly IfxTableGetter ifxTableGetter;
		private readonly IfxTableSetter ifxTableSetter;
		private readonly DuplexGetter duplexGetter;

		public IfxTableProcessor(SLProtocol protocol)
		{
			this.protocol = protocol;

			ifxTableGetter = new IfxTableGetter(protocol);
			ifxTableGetter.Load();
			duplexGetter = new DuplexGetter(protocol);
			duplexGetter.Load();

			ifxTableSetter = new IfxTableSetter(protocol);
		}

		public void ProcessData()
		{
			SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId, Parameter.interfacesratecalculationsmethod);

			Dictionary<string, DuplexStatus> duplexStatuses = ConvertDuplexColumnToDictionary();

			for (int i = 0; i < ifxTableGetter.Keys.Length; i++)
			{
				// Key
				string key = Convert.ToString(ifxTableGetter.Keys[i]);
				ifxTableSetter.SetColumnsData[Parameter.Ifxtable.tablePid].Add(key);

				// Rates
				ProcessBitRates(snmpDeltaHelper, i, out double bitrateIn, out double bitrateOut);

				// Utilization
				ProcessUtilization(duplexStatuses, i, key, bitrateIn, bitrateOut);
			}

			if (ifxTableGetter.IsSnmpAgentRestarted)
			{
				ifxTableSetter.SetParamsData[Parameter.ifxtablesnmpagentrestartflag] = 0;
			}
		}

		public void UpdateProtocol()
		{
			ifxTableSetter.SetColumns();
			ifxTableSetter.SetParams();
		}

		private static double CalculateBitRate(string key, ulong octectCount, SnmpDeltaHelper snmpDeltaHelper, SnmpRate64 snmpRateHelper)
		{
			double octetRate = snmpRateHelper.Calculate(snmpDeltaHelper, octectCount, key);
			double bitRate = octetRate > 0 ? octetRate * 8 : octetRate;

			return bitRate;
		}

		private void ProcessBitRates(SnmpDeltaHelper snmpDeltaHelper, int getPosition, out double bitrateIn, out double bitrateOut)
		{
			string key = Convert.ToString(ifxTableGetter.Keys[getPosition]);

			string serializedIfxRateData = Convert.ToString(ifxTableGetter.RateData[getPosition]);
			InterfaceRateData64 rateData = InterfaceRateData64.FromJsonString(serializedIfxRateData, MinDelta, MaxDelta);

			string currentDiscontinuity = Convert.ToString(ifxTableGetter.Discontinuity[getPosition]);
			bool discontinuity = InterfaceDiscontinuityHelper.HasDiscontinuity(currentDiscontinuity, rateData.PreviousDiscontinuity);

			if (ifxTableGetter.IsSnmpAgentRestarted || discontinuity)
			{
				rateData.BitrateInData = SnmpRate64.FromJsonString(String.Empty, MinDelta, MaxDelta);
				rateData.BitrateOutData = SnmpRate64.FromJsonString(String.Empty, MinDelta, MaxDelta);
			}

			ulong octetsIn = SafeConvert.ToUInt64(Convert.ToDouble(ifxTableGetter.OctetsIn[getPosition]));
			bitrateIn = CalculateBitRate(key, octetsIn, snmpDeltaHelper, rateData.BitrateInData);

			ulong octetsOut = SafeConvert.ToUInt64(Convert.ToDouble(ifxTableGetter.OctetsOut[getPosition]));
			bitrateOut = CalculateBitRate(key, octetsOut, snmpDeltaHelper, rateData.BitrateOutData);

			ifxTableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifinbitrate].Add(bitrateIn);
			ifxTableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifoutbitrate].Add(bitrateOut);
			ifxTableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifratedata].Add(rateData.ToJsonString());
		}

		private void ProcessUtilization(Dictionary<string, DuplexStatus> duplexStatuses, int getPosition, string key, double bitrateIn, double bitrateOut)
		{
			double speedValue = GetSpeedValue(getPosition);

			DuplexStatus duplexStatus = duplexStatuses.ContainsKey(key)
				? duplexStatuses[key]
				: DuplexStatus.NotInitialized;

			double utilization = InterfaceUtilizationHelper.CalculateUtilization(bitrateIn, bitrateOut, speedValue, duplexStatus);

			ifxTableSetter.SetColumnsData[Parameter.Ifxtable.Pid.ifxtableifbandwidthutilization].Add(utilization);
		}

		private double GetSpeedValue(int getPosition)
		{
			uint speedInTable = SafeConvert.ToUInt32(Convert.ToDouble(ifxTableGetter.Speed[getPosition]));

			double speedValueToUse = Convert.ToDouble(speedInTable) * Math.Pow(10, 6);
			return speedValueToUse;
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
				uint[] columnsToGet = new uint[]
				{
					Parameter.Dot3statstable.Idx.dot3statsindex,
					Parameter.Dot3statstable.Idx.dot3statsduplexstatus,
				};

				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Dot3statstable.tablePid, columnsToGet);

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

			public object[] Speed { get; private set; }

			public object[] Discontinuity { get; private set; }

			public object[] RateData { get; private set; }

			public bool IsSnmpAgentRestarted { get; private set; }

			public void Load()
			{
				IsSnmpAgentRestarted = Convert.ToBoolean(Convert.ToInt16(protocol.GetParameter(Parameter.ifxtablesnmpagentrestartflag)));

				uint[] columnsToGet = new uint[]
				{
					Parameter.Ifxtable.Idx.ifxtableifindex,
					Parameter.Ifxtable.Idx.ifxtableifhcinoctets,
					Parameter.Ifxtable.Idx.ifxtableifhcoutoctets,
					Parameter.Ifxtable.Idx.ifxtableifhighspeed,
					Parameter.Ifxtable.Idx.ifxtableifcounterdiscontinuitytime,
					Parameter.Ifxtable.Idx.ifxtableifratedata,
				};

				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Ifxtable.tablePid, columnsToGet);

				Keys = (object[])tableData[0];
				OctetsIn = (object[])tableData[1];
				OctetsOut = (object[])tableData[2];
				Speed = (object[])tableData[3];
				Discontinuity = (object[])tableData[4];
				RateData = (object[])tableData[5];
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
				protocol.SetParams(SetParamsData);
			}
		}
	}
}