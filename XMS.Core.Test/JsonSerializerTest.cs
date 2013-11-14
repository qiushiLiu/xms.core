using XMS.Core.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace XMS.Core.Test
{
    
    
    /// <summary>
    ///This is a test class for JsonSerializerTest and is intended
    ///to contain all JsonSerializerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class JsonSerializerTest
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


        internal class Boo<T>
        {
            public T boo { get; set; }
            public string msg { get; set; }
        }

        /// <summary>
        ///A test for Deserialize
        ///</summary>
        public void DeserializeTestHelper<T>()
        {
            string input = "{\"boo\":true,\"msg\":\"\"}"; ; // TODO: Initialize to an appropriate value
            Boo<T> actual = JsonSerializer.Deserialize<Boo<T>>(input);
            Assert.IsTrue(actual.boo);
        }
    }
}
