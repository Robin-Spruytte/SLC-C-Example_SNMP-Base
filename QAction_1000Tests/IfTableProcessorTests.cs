namespace Skyline.Protocol.IfTable.Tests
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	using Skyline.DataMiner.Library.Common.Rates;

	[TestClass]
	public class IfTableProcessorTests
	{
		[TestMethod]
		public void CalculateUtilizationTest_InvalidInputRate()
		{
			Assert.AreEqual(-1, IfTableProcessor.CalculateUtilization(-1, 2000, 5000, DuplexStatus.FullDuplex));
		}

		[TestMethod]
		public void CalculateUtilizationTest_InvalidOutputRate()
		{
			Assert.AreEqual(-1, IfTableProcessor.CalculateUtilization(1500, -1, 5000, DuplexStatus.FullDuplex));
		}

		[TestMethod]
		public void CalculateUtilizationTest_InvalidSpeed()
		{
			Assert.AreEqual(-1, IfTableProcessor.CalculateUtilization(1500, 2000, 0, DuplexStatus.FullDuplex));
		}

		[TestMethod]
		public void CalculateUtilizationTest_InvalidDuplexStatus_NA()
		{
			Assert.AreEqual(-1, IfTableProcessor.CalculateUtilization(1500, 2000, 0, DuplexStatus.NotInitialized));
		}

		[TestMethod]
		public void CalculateUtilizationTest_InvalidDuplexStatus_Unknown()
		{
			Assert.AreEqual(-1, IfTableProcessor.CalculateUtilization(1500, 2000, 0, DuplexStatus.Unknown));
		}

		[TestMethod]
		public void CalculateUtilizationTest_HalfDuplex()
		{
			Assert.AreEqual(70, IfTableProcessor.CalculateUtilization(1500, 2000, 5000, DuplexStatus.HalfDuplex));
		}

		[TestMethod]
		public void CalculateUtilizationTest_FullDuplex()
		{
			Assert.AreEqual(40, IfTableProcessor.CalculateUtilization(1500, 2000, 5000, DuplexStatus.FullDuplex));
		}

		[TestMethod]
		public void CheckDiscontinuity_True()
		{
			Assert.IsTrue(IfTableProcessor.CheckDiscontinuity("0", "1"));
		}

		[TestMethod]
		public void CheckDiscontinuity_False()
		{
			Assert.IsFalse(IfTableProcessor.CheckDiscontinuity("0", "0"));
		}
	}
}