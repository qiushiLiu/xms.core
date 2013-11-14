using XMS.Core.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using XMS.Core.Pay;

namespace XMS.Core.Test
{
    /// <summary>
    ///This is a test class for StringExtendTest and is intended
    ///to contain all StringExtendTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ErrorCodeTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        /// <summary>
        ///A test for ToHtmlEncode
        ///</summary>
        [TestMethod()]
		public void Test()
        {
			Exception err = ErrorCodeHelper.test.ToException(new { A = "a", B = 1, C = 2 });

			Assert.IsNotNull(err);

       }

		private class ErrorCodeHelper
		{
			public static ErrorCode test = new ErrorCode("test", 1001, "hi {A}, enter your {{{B}}}, then click {{C}}, good luck, {C}!{");
		}
    }
}
