namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	public static class InterfaceUtilizationHelper
	{
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
	}
}