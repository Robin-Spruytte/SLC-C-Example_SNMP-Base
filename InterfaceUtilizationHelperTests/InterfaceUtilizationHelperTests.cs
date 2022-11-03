using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Skyline.DataMiner.Library.Common.Rates.Tests
{
    [TestClass()]
    public class InterfaceUtilizationHelperTests
    {
        [TestMethod()]
        public void CalculateUtilizationTest_InvalidInputRate()
        {
            Assert.AreEqual(-1, InterfaceUtilizationHelper.CalculateUtilization(-1, 2000, 5000, DuplexStatus.FullDuplex));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_InvalidOutputRate()
        {
            Assert.AreEqual(-1, InterfaceUtilizationHelper.CalculateUtilization(1500, -1, 5000, DuplexStatus.FullDuplex));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_InvalidSpeed()
        {
            Assert.AreEqual(-1, InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.FullDuplex));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_InvalidDuplexStatus_NA()
        {
            Assert.AreEqual(-1, InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.NotInitialized));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_InvalidDuplexStatus_Unknown()
        {
            Assert.AreEqual(-1, InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 0, DuplexStatus.Unknown));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_HalfDuplex()
        {
            Assert.AreEqual(70, InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 5000, DuplexStatus.HalfDuplex));
        }

        [TestMethod()]
        public void CalculateUtilizationTest_FullDuplex()
        {
            Assert.AreEqual(40, InterfaceUtilizationHelper.CalculateUtilization(1500, 2000, 5000, DuplexStatus.FullDuplex));
        }
    }
}