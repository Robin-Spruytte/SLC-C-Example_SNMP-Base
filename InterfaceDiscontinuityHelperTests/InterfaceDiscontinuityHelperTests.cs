using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Skyline.DataMiner.Library.Common.Rates.Tests
{
	[TestClass]
	public class InterfaceDiscontinuityHelperTests
	{
		[TestMethod]
		public void CheckDiscontinuity_True()
		{
			bool expected = true;
			bool result = InterfaceDiscontinuityHelper.HasDiscontinuity("0", "1");

			Assert.AreEqual(expected, result);
		}

		[TestMethod]
		public void CheckDiscontinuity_False()
		{
			bool expected = false;
			bool result = InterfaceDiscontinuityHelper.HasDiscontinuity("0", "0");

			Assert.AreEqual(expected, result);
		}
	}
}