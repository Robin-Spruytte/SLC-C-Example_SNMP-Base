namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;

	public class InterfaceRateData64
	{
		public SnmpRate64 BitrateIn { get; set; }

		public SnmpRate64 BitrateOut { get; set; }

		public string DiscontinuityTime { get; set; }

		public static InterfaceRateData64 FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new InterfaceRateData64
				{
					BitrateIn = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOut = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					DiscontinuityTime = String.Empty,
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