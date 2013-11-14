using XMS.Core.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace XMS.Core.Test
{
    
    
    /// <summary>
    ///This is a test class for WebRequestHelperTest and is intended
    ///to contain all WebRequestHelperTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WebRequestHelperTest
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

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for GetWebData
        ///</summary>
        [TestMethod()]
        public void GetWebDataTest()
        {
           
            string sUrl = "http://www.taobao.com"; // TODO: Initialize to an appropriate value
            int nTimeOut = 1000; // TODO: Initialize to an appropriate value
            bool bIsUseGzip = false; // TODO: Initialize to an appropriate value

            DownloadRslt actual;
            actual = WebRequestHelper.GetWebData(sUrl, nTimeOut, bIsUseGzip, null);
            string s = System.Text.Encoding.UTF8.GetString(actual.Content);
            string z = s;
            Assert.AreEqual(null, actual);
           
        }

        /// <summary>
        ///A test for GetWebDataString
        ///</summary>
        [TestMethod()]
        public void GetWebDataStringTest()
        {

            string sUrl = "http://www.57hao.com/common/fgheader.aspx?CityId=200000"; // TODO: Initialize to an appropriate value
            int nTimeOut =1000; // TODO: Initialize to an appropriate value
            bool bIsUseGzip = true; // TODO: Initialize to an appropriate value
            string expected = string.Empty; // TODO: Initialize to an appropriate value
            string actual;
            actual = WebRequestHelper.GetWebDataString(sUrl, nTimeOut, bIsUseGzip, null);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
