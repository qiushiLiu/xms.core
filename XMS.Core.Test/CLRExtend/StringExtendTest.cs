using XMS.Core.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace XMS.Core.Test
{


    /// <summary>
    ///This is a test class for StringExtendTest and is intended
    ///to contain all StringExtendTest Unit Tests
    ///</summary>
    [TestClass()]
    public class StringExtendTest
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
        ///A test for ToHtmlEncode
        ///</summary>
        [TestMethod()]
        public void ToHtmlEncodeTest()
        {
            string str = "测试<a href=\"dd\"></a>\r\n对对对\n放大沙发大厦\rfds\"'"; // TODO: Initialize to an appropriate value
            bool bReplaceNewline = false; // TODO: Initialize to an appropriate value
            string expected = string.Empty; // TODO: Initialize to an appropriate value
            string actual = str.ToHtmlEncode(true);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
