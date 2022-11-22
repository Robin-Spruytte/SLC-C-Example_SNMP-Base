namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	public static class InterfaceUtilizationHelper
	{
		/// <summary>
		/// Calculates an interface utilization.
		/// </summary>
		/// <param name="inputRate">The input rate of the interface.<br/>
		/// The unit should be consistent between <paramref name="inputRate"/>, <paramref name="outputRate"/> and <paramref name="interfaceSpeed"/>.</param>
		/// <param name="outputRate">The output rate of the interface.<br/>
		/// The unit should be consistent between <paramref name="inputRate"/>, <paramref name="outputRate"/> and <paramref name="interfaceSpeed"/>.</param>
		/// <param name="interfaceSpeed">The speed of the interface (the max rate supported).<br/>
		/// The unit should be consistent between <paramref name="inputRate"/>, <paramref name="outputRate"/> and <paramref name="interfaceSpeed"/>.</param>
		/// <param name="duplexStatus">The current mode of operation of the MAC entity.</param>
		/// <returns>The interface utilization in percent.</returns>
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