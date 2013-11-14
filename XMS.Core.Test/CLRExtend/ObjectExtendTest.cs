using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XMS.Core.Test.CLRExtend
{
	public class A
	{
		public string A1 = "a1";

		private string a2 = "a2";
		public string A2
		{
			get
			{
				return this.a2;
			}
			set
			{
				this.a2 = value;
			}
		}

		private string A3 = "a3";
	}

	public class B
	{
		public string A1 = "b1";

		private string a2 = "b2";
		public string A2
		{
			get
			{
				return this.a2;
			}
			set
			{
				this.a2 = value;
			}
		}

		private string A3 = "b3";

		public string B1 = "b1";

		private string b2 = "b2";
		public string B2
		{
			get
			{
				return this.b2;
			}
			set
			{
				this.b2 = value;
			}
		}

		private string B3 = "b3";
	}

	/// <summary>
	/// ObjectExtendTest 的摘要说明
	/// </summary>
	[TestClass]
	public class ObjectExtendTest
	{
		public ObjectExtendTest()
		{
			//
			//TODO: 在此处添加构造函数逻辑
			//
		}

		private TestContext testContextInstance;

		/// <summary>
		///获取或设置测试上下文，该上下文提供
		///有关当前测试运行及其功能的信息。
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

		#region 附加测试特性
		//
		// 编写测试时，可以使用以下附加特性:
		//
		// 在运行类中的第一个测试之前使用 ClassInitialize 运行代码
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// 在类中的所有测试都已运行之后使用 ClassCleanup 运行代码
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// 在运行每个测试之前，使用 TestInitialize 来运行代码
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// 在每个测试运行完之后，使用 TestCleanup 来运行代码
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		[TestMethod]
		public void MemberwiseCopy()
		{
			A a = new A();
			B b = new B();

			B c = b.MemberwiseClone();

			A d = a.MemberwiseClone();

			Assert.IsTrue(d.A1 == a.A1);
			Assert.IsTrue(c.A1 == b.A1);
		}
	}
}
