namespace Skyline.Protocol.IfTable
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Library.Common.Rates;
	using Skyline.DataMiner.Library.Common.SafeConverters;
	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;
	using Skyline.DataMiner.Scripting;
	using Skyline.Protocol.Extensions;

	public class IfTableTimeoutProcessor
	{
		private const int GroupId = 1000;
		private static readonly TimeSpan MinDelta = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan MaxDelta = new TimeSpan(0, 10, 0);

		private readonly SLProtocol protocol;

		private readonly IfTableGetter iftableGetter;
		private readonly IfTableSetter iftableSetter;

		public IfTableTimeoutProcessor(SLProtocol protocol)
		{
			this.protocol = protocol;

			iftableGetter = new IfTableGetter(protocol);
			iftableGetter.Load();

			iftableSetter = new IfTableSetter(protocol);
		}

		public void ProcessTimeout()
		{
			SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId, Parameter.interfacesratecalculationsmethod);

			for (int i = 0; i < iftableGetter.Keys.Length; i++)
			{
				string key = Convert.ToString(iftableGetter.Keys[i]);
				string serializedIfRateData = Convert.ToString(iftableGetter.IfRateData[i]);

				InterfaceRateData32 rateData = InterfaceRateData32.FromJsonString(serializedIfRateData, MinDelta, MaxDelta);

				rateData.BitrateInData.BufferDelta(snmpDeltaHelper, key);
				rateData.BitrateOutData.BufferDelta(snmpDeltaHelper, key);

				iftableSetter.SetColumnsData[Parameter.Iftable.tablePid].Add(key);
				iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifratedata].Add(rateData.ToJsonString());
			}
		}

		public void UpdateProtocol()
		{
			iftableSetter.SetColumns();
		}

		private class IfTableGetter
		{
			private readonly SLProtocol protocol;

			public IfTableGetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public object[] Keys { get; private set; }

			public object[] IfRateData { get; private set; }

			public void Load()
			{
				uint[] columnsToGet = new uint[]
				{
					Parameter.Iftable.Idx.iftableifindex,
					Parameter.Iftable.Idx.iftableifratedata,
				};

				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Iftable.tablePid, columnsToGet);

				Keys = (object[])tableData[0];
				IfRateData = (object[])tableData[1];
			}
		}

		private class IfTableSetter
		{
			private readonly SLProtocol protocol;

			public IfTableSetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public Dictionary<object, List<object>> SetColumnsData { get; } = new Dictionary<object, List<object>>
			{
				{ Parameter.Iftable.tablePid, new List<object>() },
				{ Parameter.Iftable.Pid.iftableifratedata, new List<object>() },
			};

			public void SetColumns()
			{
				protocol.SetColumns(SetColumnsData);
			}
		}
	}

	public class IfTableProcessor
	{
		private const int GroupId = 1000;
		private static readonly TimeSpan MinDelta = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan MaxDelta = new TimeSpan(0, 10, 0);

		private readonly SLProtocol protocol;

		private readonly IfTableGetter iftableGetter;
		private readonly IfTableSetter iftableSetter;
		private readonly DuplexGetter duplexGetter;

		public IfTableProcessor(SLProtocol protocol)
		{
			this.protocol = protocol;

			iftableGetter = new IfTableGetter(protocol);
			iftableGetter.Load();
			duplexGetter = new DuplexGetter(protocol);
			duplexGetter.Load();

			iftableSetter = new IfTableSetter(protocol);
		}

		public void ProcessData()
		{
			SnmpDeltaHelper snmpDeltaHelper = new SnmpDeltaHelper(protocol, GroupId, Parameter.interfacesratecalculationsmethod);

			Dictionary<string, DuplexStatus> duplexStatuses = ConvertDuplexColumnToDictionary();

			for (int i = 0; i < iftableGetter.Keys.Length; i++)
			{
				// Key
				string key = Convert.ToString(iftableGetter.Keys[i]);
				iftableSetter.SetColumnsData[Parameter.Iftable.tablePid].Add(key);

				// Rates
				ProcessBitRates(snmpDeltaHelper, i, out double bitrateIn, out double bitrateOut);

				// Utilization
				ProcessUtilization(duplexStatuses, i, key, bitrateIn, bitrateOut);
			}

			if (iftableGetter.IsSnmpAgentRestarted)
			{
				iftableSetter.SetParamsData[Parameter.iftablesnmpagentrestartflag] = 0;
			}
		}

		public void UpdateProtocol()
		{
			iftableSetter.SetColumns();
			iftableSetter.SetParams();
		}

		private static double CalculateBitRate(string key, uint octectCount, SnmpDeltaHelper snmpDeltaHelper, SnmpRate32 snmpRateHelper)
		{
			double octetRate = snmpRateHelper.Calculate(snmpDeltaHelper, octectCount, key);
			double bitRate = octetRate > 0 ? octetRate * 8 : octetRate;

			return bitRate;
		}

		private void ProcessBitRates(SnmpDeltaHelper snmpDeltaHelper, int getPosition, out double bitrateIn, out double bitrateOut)
		{
			string key = Convert.ToString(iftableGetter.Keys[getPosition]);

			string serializedIfRateData = Convert.ToString(iftableGetter.RateData[getPosition]);
			InterfaceRateData32 rateData = InterfaceRateData32.FromJsonString(serializedIfRateData, MinDelta, MaxDelta);

			string currentDiscontinuity = Convert.ToString(iftableGetter.Discontinuity[getPosition]);
			bool discontinuity = InterfaceDiscontinuityHelper.HasDiscontinuity(currentDiscontinuity, rateData.PreviousDiscontinuity);

			if (iftableGetter.IsSnmpAgentRestarted || discontinuity)
			{
				rateData.BitrateInData = SnmpRate32.FromJsonString(String.Empty, MinDelta, MaxDelta);
				rateData.BitrateOutData = SnmpRate32.FromJsonString(String.Empty, MinDelta, MaxDelta);
			}

			uint octetsIn = SafeConvert.ToUInt32(Convert.ToDouble(iftableGetter.OctetsIn[getPosition]));
			bitrateIn = CalculateBitRate(key, octetsIn, snmpDeltaHelper, rateData.BitrateInData);

			uint octetsOut = SafeConvert.ToUInt32(Convert.ToDouble(iftableGetter.OctetsOut[getPosition]));
			bitrateOut = CalculateBitRate(key, octetsOut, snmpDeltaHelper, rateData.BitrateOutData);

			iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifinbitrate].Add(bitrateIn);
			iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifoutbitrate].Add(bitrateOut);
			iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifratedata].Add(rateData.ToJsonString());
		}

		private void ProcessUtilization(Dictionary<string, DuplexStatus> duplexStatuses, int getPosition, string key, double bitrateIn, double bitrateOut)
		{
			double speedValue = GetSpeedValue(getPosition);

			DuplexStatus duplexStatus = duplexStatuses.ContainsKey(key)
				? duplexStatuses[key]
				: DuplexStatus.NotInitialized;

			double utilization = InterfaceUtilizationHelper.CalculateUtilization(bitrateIn, bitrateOut, speedValue, duplexStatus);

			iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifbandwidthutilization].Add(utilization);
		}

		private double GetSpeedValue(int getPosition)
		{
			uint speedInTable = SafeConvert.ToUInt32(Convert.ToDouble(iftableGetter.Speed[getPosition]));

			return speedInTable == UInt32.MaxValue
								? -1.0
								: Convert.ToDouble(speedInTable);
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

		private class IfTableGetter
		{
			private readonly SLProtocol protocol;

			public IfTableGetter(SLProtocol protocol)
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
				IsSnmpAgentRestarted = Convert.ToBoolean(Convert.ToInt16(protocol.GetParameter(Parameter.iftablesnmpagentrestartflag)));

				LoadIfTable();
				LoadIfXTable();
			}

			private void LoadIfTable()
			{
				List<uint> columnsToGet = new List<uint>
				{
					Parameter.Iftable.Idx.iftableifindex,
					Parameter.Iftable.Idx.iftableifinoctets,
					Parameter.Iftable.Idx.iftableifoutoctets,
					Parameter.Iftable.Idx.iftableifspeed,
				};

				if (!IsSnmpAgentRestarted)
				{
					columnsToGet.Add(Parameter.Iftable.Idx.iftableifratedata);
				}

				object[] ifTableData = (object[])protocol.NotifyProtocol(321, Parameter.Iftable.tablePid, columnsToGet.ToArray());

				Keys = (object[])ifTableData[0];
				OctetsIn = (object[])ifTableData[1];
				OctetsOut = (object[])ifTableData[2];
				Speed = (object[])ifTableData[3];
				Discontinuity = new object[Keys.Length];    // Will be filled in via LoadIfXTable

				if (!IsSnmpAgentRestarted)
				{
					RateData = (object[])ifTableData[4];
				}
			}

			private void LoadIfXTable()
			{
				List<uint> columnsToGet = new List<uint>
				{
					Parameter.Ifxtable.Idx.ifxtableifindex,
					Parameter.Ifxtable.Idx.ifxtableifcounterdiscontinuitytime,
				};

				object[] ifXTableData = (object[])protocol.NotifyProtocol(321, Parameter.Ifxtable.tablePid, columnsToGet.ToArray());
				object[] ifXTableKeys = (object[])ifXTableData[0];
				object[] ifXTableDiscontinuities = (object[])ifXTableData[1];

				for (int i = 0; i < ifXTableKeys.Length; i++)
				{
					int position = Array.IndexOf(Keys, ifXTableKeys[i]);
					if (position > -1)
					{
						Discontinuity[position] = ifXTableDiscontinuities[i];
					}
				}
			}
		}

		private class IfTableSetter
		{
			private readonly SLProtocol protocol;

			public IfTableSetter(SLProtocol protocol)
			{
				this.protocol = protocol;
			}

			public Dictionary<object, List<object>> SetColumnsData { get; } = new Dictionary<object, List<object>>
			{
				{ Parameter.Iftable.tablePid, new List<object>() },
				{ Parameter.Iftable.Pid.iftableifinbitrate, new List<object>() },
				{ Parameter.Iftable.Pid.iftableifoutbitrate, new List<object>() },
				{ Parameter.Iftable.Pid.iftableifbandwidthutilization, new List<object>() },
				{ Parameter.Iftable.Pid.iftableifratedata, new List<object>() },
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