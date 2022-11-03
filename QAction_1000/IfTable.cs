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
				string streamPk = Convert.ToString(iftableGetter.Keys[i]);
				string serializedIfRateData = Convert.ToString(iftableGetter.IfRateData[i]);

				InterfaceRateData32 rateData = InterfaceRateData32.FromJsonString(serializedIfRateData, MinDelta, MaxDelta);

				rateData.BitrateInData.BufferDelta(snmpDeltaHelper, streamPk);
				rateData.BitrateOutData.BufferDelta(snmpDeltaHelper, streamPk);

				iftableSetter.SetColumnsData[Parameter.Iftable.tablePid].Add(streamPk);
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
				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Iftable.tablePid, new uint[]
				{
					Parameter.Iftable.Idx.iftableifindex,
					Parameter.Iftable.Idx.iftableifratedata,
				});

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
				string key = Convert.ToString(iftableGetter.Keys[i]);
				uint speedInTable = Convert.ToUInt32(iftableGetter.Speed[i]);
				string serializedIfRateData = Convert.ToString(iftableGetter.IfRateData[i]);

				double speedValueToUse = speedInTable == UInt32.MaxValue
					? -1.0
					: Convert.ToDouble(speedInTable);

				DuplexStatus duplexStatus = duplexStatuses.ContainsKey(key)
					? duplexStatuses[key]
					: DuplexStatus.NotInitialized;

				iftableSetter.SetColumnsData[Parameter.Iftable.tablePid].Add(Convert.ToString(iftableGetter.Keys[i]));

				InterfaceRateData32 rateData = InterfaceRateData32.FromJsonString(serializedIfRateData, MinDelta, MaxDelta);
				string currentDiscontinuity = Convert.ToString(iftableGetter.Discontinuity[i]);
				bool discontinuity = InterfaceDiscontinuityHelper.CheckDiscontinuity(currentDiscontinuity, rateData.PreviousDiscontinuity);

				iftableSetter.SetParamsData[Parameter.iftablesnmpagentrestartflag] = 0;
				if (iftableGetter.IsSnmpAgentRestarted || discontinuity)
				{
					rateData.BitrateInData = SnmpRate32.FromJsonString(String.Empty, MinDelta, MaxDelta);
					rateData.BitrateOutData = SnmpRate32.FromJsonString(String.Empty, MinDelta, MaxDelta);
				}

				double bitrateIn = CalculateBitrateIn(i, snmpDeltaHelper, rateData.BitrateInData, discontinuity);
				double bitrateOut = CalculateBitrateOut(i, snmpDeltaHelper, rateData.BitrateOutData, discontinuity);
				double utilization = InterfaceUtilizationHelper.CalculateUtilization(bitrateIn, bitrateOut, speedValueToUse, duplexStatus);

				iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifinbitrate].Add(bitrateIn);
				iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifoutbitrate].Add(bitrateOut);
				iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifbandwidthutilization].Add(utilization);
				iftableSetter.SetColumnsData[Parameter.Iftable.Pid.iftableifratedata].Add(rateData.ToJsonString());
			}
		}

		public void UpdateProtocol()
		{
			iftableSetter.SetColumns();
			iftableSetter.SetParams();
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

		private double CalculateBitrateIn(int getPosition, SnmpDeltaHelper snmpDeltaHelper, SnmpRate32 snmpRateHelper, bool discontinuity)
		{
			if (discontinuity)
			{
				return -1;
			}

			string streamPk = Convert.ToString(iftableGetter.Keys[getPosition]);
			uint octetsIn = SafeConvert.ToUInt32(Convert.ToDouble(iftableGetter.OctetsIn[getPosition]));

			double octetRateIn = snmpRateHelper.Calculate(snmpDeltaHelper, octetsIn, streamPk);
			double bitRateIn = octetRateIn > 0 ? octetRateIn / 8 : octetRateIn;

			return bitRateIn;
		}

		private double CalculateBitrateOut(int getPosition, SnmpDeltaHelper snmpDeltaHelper, SnmpRate32 snmpRateHelper, bool discontinuity)
		{
			if (discontinuity)
			{
				return -1;
			}

			string streamPk = Convert.ToString(iftableGetter.Keys[getPosition]);
			uint octetsOut = SafeConvert.ToUInt32(Convert.ToDouble(iftableGetter.OctetsOut[getPosition]));

			double octetRateOut = snmpRateHelper.Calculate(snmpDeltaHelper, octetsOut, streamPk);
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
				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Dot3statstable.tablePid, new uint[]
				{
					Parameter.Dot3statstable.Idx.dot3statsindex,
					Parameter.Dot3statstable.Idx.dot3statsduplexstatus,
				});

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

			public object[] IfRateData { get; private set; }

			public bool IsSnmpAgentRestarted { get; private set; }

			public void Load()
			{
				IsSnmpAgentRestarted = Convert.ToBoolean(Convert.ToInt16(protocol.GetParameter(Parameter.iftablesnmpagentrestartflag)));

				List<uint> columnsToGet = new List<uint>
				{
					Parameter.Iftable.Idx.iftableifindex,
					Parameter.Iftable.Idx.iftableifinoctets,
					Parameter.Iftable.Idx.iftableifoutoctets,
					Parameter.Iftable.Idx.iftableifspeed,
					Parameter.Iftable.Idx.iftableifcounterdiscontinuitytime,
				};

				if (!IsSnmpAgentRestarted)
				{
					columnsToGet.Add(Parameter.Iftable.Idx.iftableifratedata);
				}

				object[] tableData = (object[])protocol.NotifyProtocol(321, Parameter.Iftable.tablePid, columnsToGet.ToArray());

				Keys = (object[])tableData[0];
				OctetsIn = (object[])tableData[1];
				OctetsOut = (object[])tableData[2];
				Speed = (object[])tableData[3];
				Discontinuity = (object[])tableData[4];

				if (!IsSnmpAgentRestarted)
				{
					IfRateData = (object[])tableData[5];
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