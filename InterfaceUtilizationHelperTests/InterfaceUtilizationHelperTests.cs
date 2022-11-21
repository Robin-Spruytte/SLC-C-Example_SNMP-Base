namespace Skyline.DataMiner.Library.Common.Rates.Tests
{
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
    public class InterfaceUtilizationHelperTests
    {
        [TestMethod]
        public void CalculateUtilizationTest_InvalidInputRate()
        {
			double expected = -1;
			double result = InterfaceUtilizationHelper.CalculateUtilization(-1, 2000, 5000, DuplexStatus.FullDuplex);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_InvalidOutputRate()
		{
			double expected = -1;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, -1, 5000, DuplexStatus.FullDuplex);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_InvalidSpeed()
		{
			double expected = -1;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.FullDuplex);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_InvalidDuplexStatus_NA()
		{
			double expected = -1;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.NotInitialized);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_InvalidDuplexStatus_Unknown()
		{
			double expected = -1;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.Unknown);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_HalfDuplex()
		{
			double expected = 70;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 5000, DuplexStatus.HalfDuplex);

			Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void CalculateUtilizationTest_FullDuplex()
		{
			double expected = 40;
			double result = InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 5000, DuplexStatus.FullDuplex);

			Assert.AreEqual(expected, result);
        }
    }
}