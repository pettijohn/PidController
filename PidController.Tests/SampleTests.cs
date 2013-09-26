using System;
using Microsoft.SPOT;
using MFUnit;

namespace PidController.Tests
{
    public class SampleTests
    {
        /// <summary>
        /// All public void methods in classes named ^.*Tests$ are treated as tests. 
        /// </summary>
        public void AssertIsNull_ShouldPass_WhenActualIsNull()
        {
            Assert.IsNull(null);
        }
    }
}
