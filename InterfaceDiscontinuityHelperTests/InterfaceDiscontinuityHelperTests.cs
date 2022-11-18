using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Skyline.DataMiner.Library.Common.Rates.Tests
{
    [TestClass()]
    public class InterfaceDiscontinuityHelperTests
    {
        [TestMethod()]
        public void CheckDiscontinuity_True()
        {
            Assert.IsTrue(InterfaceDiscontinuityHelper.HasDiscontinuity("0", "1"));
        }

        [TestMethod()]
        public void CheckDiscontinuity_False()
        {
            Assert.IsFalse(InterfaceDiscontinuityHelper.HasDiscontinuity("0", "0"));
        }
    }
}