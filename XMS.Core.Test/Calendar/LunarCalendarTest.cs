using XMS.Core.Calendar;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace XMS.Core.Test
{
    
    
    /// <summary>
    ///This is a test class for LunarCalendarTest and is intended
    ///to contain all LunarCalendarTest Unit Tests
    ///</summary>
    [TestClass()]
    public class LunarCalendarTest
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
        ///A test for GetDate
        ///</summary>
        [TestMethod()]
        public void GetDateTest()
        {
            LunarCalendar target = new LunarCalendar(); // TODO: Initialize to an appropriate value
            DateTime dtSolarDate = new DateTime(2011,4,5); // TODO: Initialize to an appropriate value
            ResultDate expected = null; // TODO: Initialize to an appropriate value
            ResultDate actual;
            actual = target.GetDate(dtSolarDate);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }

        /// <summary>
        ///A test for FindDate
        ///</summary>
        [TestMethod()]
        public void FindDateTest()
        {
            LunarCalendar target = new LunarCalendar(); // TODO: Initialize to an appropriate value
            DateTime dtStartDate = new DateTime(2011, 4, 5); // TODO: Initialize to an appropriate value
            DateTime dtEndDate = new DateTime(2031, 4, 5); // TODO: Initialize to an appropriate value
            List<ResultDate> expected = null; // TODO: Initialize to an appropriate value
            List<ResultDate> actual;
            actual = target.FindDate(dtStartDate, dtEndDate);
            Assert.AreEqual(expected, actual);
            Assert.Inconclusive("Verify the correctness of this test method.");
        }
    }
}
