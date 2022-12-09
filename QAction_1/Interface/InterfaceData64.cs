namespace Skyline.Protocol.Interface
{
	using System;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Utils.Rates.Common;
	using Skyline.DataMiner.Utils.Rates.Protocol;

	public class InterfaceData64
	{
		public SnmpRate64 BitrateIn { get; set; }

		public SnmpRate64 BitrateOut { get; set; }

		public string DiscontinuityTime { get; set; }

		public static InterfaceData64 FromJsonString(string serializedIfxRateData, TimeSpan minDelta, TimeSpan maxDelta, RateBase rateBase = RateBase.Second)
		{
			if (String.IsNullOrWhiteSpace(serializedIfxRateData))
			{
				return new InterfaceData64
				{
					BitrateIn = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					BitrateOut = SnmpRate64.FromJsonString(String.Empty, minDelta, maxDelta, rateBase),
					DiscontinuityTime = String.Empty,
				};
			}

			return JsonConvert.DeserializeObject<InterfaceData64>(serializedIfxRateData);
		}

		public string ToJsonString()
		{
			return JsonConvert.SerializeObject(this);
		}
	}
}