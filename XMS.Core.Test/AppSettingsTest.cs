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
    public class AppSettingsTest
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
        public void ServiceTest()
        {
			for (int j = 0; j < 1000000; j++)
			{
				XMS.Core.Container.LogService.GetLogger("test").Info("hi " + j.ToString());

				System.Threading.Thread.Sleep(1000);
			}


			int i = XMS.Core.Container.ConfigService.GetAppSetting<int>("int", 1);

			Assert.IsTrue(i == 100);

			TimeSpan timespan = XMS.Core.Container.ConfigService.GetAppSetting<TimeSpan>("timespan", TimeSpan.MinValue);

			Assert.IsTrue(timespan == TimeSpan.FromHours(1));

			String str = XMS.Core.Container.ConfigService.GetAppSetting<String>("string", "");

			Assert.IsTrue(str == "test");


			bool b = XMS.Core.Container.ConfigService.GetAppSetting<bool>("bool", true);

			Assert.IsTrue(b == false);

       }
    }
}
