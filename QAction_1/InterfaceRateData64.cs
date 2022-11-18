namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;

	public class InterfaceRateData64
	{
		public SnmpRate64 BitrateInData { get; set; }

		public SnmpRate64 BitrateOutData { get; set; }

		public string PreviousDiscontinuity { get; set; }

		public static InterfaceRateData64 FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new InterfaceRateData64
				{
					BitrateInData = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOutData = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					PreviousDiscontinuity = String.Empty,
				};
			}

			return JsonConvert.DeserializeObject<InterfaceRateData64>(serializedIfxRateData);
		}

		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}