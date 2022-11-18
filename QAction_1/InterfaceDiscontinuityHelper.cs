namespace Skyline.DataMiner.Library.Common.Rates
{
	using System;

	public static class InterfaceDiscontinuityHelper
	{
		public static bool HasDiscontinuity(string currentDiscontinuity, string previousDiscontinuity)
		{
			return !String.IsNullOrEmpty(previousDiscontinuity) && currentDiscontinuity != previousDiscontinuity;
		}
	}
}