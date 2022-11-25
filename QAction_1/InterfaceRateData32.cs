namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;

	public class InterfaceRateData32
	{
		public SnmpRate32 BitrateIn { get; set; }

		public SnmpRate32 BitrateOut { get; set; }

		public string DiscontinuityTime { get; set; }

		public static InterfaceRateData32 FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new InterfaceRateData32
				{
					BitrateIn = SnmpRate32.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOut = SnmpRate32.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					DiscontinuityTime = String.Empty,
				};
			}

			return JsonConvert.DeserializeObject<InterfaceRateData32>(serializedIfxRateData);
		}

		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}