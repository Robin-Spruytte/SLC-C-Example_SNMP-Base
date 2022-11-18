namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Library.Protocol.Snmp.Rates;

	public class InterfaceRateData32
	{
		public SnmpRate32 BitrateInData { get; set; }

		public SnmpRate32 BitrateOutData { get; set; }

		public string PreviousDiscontinuity { get; set; }

		public static InterfaceRateData32 FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new InterfaceRateData32
				{
					BitrateInData = SnmpRate32.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOutData = SnmpRate32.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					PreviousDiscontinuity = String.Empty,
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