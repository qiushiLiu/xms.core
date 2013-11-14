using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XMS.Core.Test
{
	/// <summary>
	/// StringInterceptorTest 的摘要说明
	/// </summary>
	[TestClass]
	public class StringInterceptorTest
	{
		public StringInterceptorTest()
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
		public void TestMethod1()
		{
			StringInterceptAttribute attr = new StringInterceptAttribute();
			//attr.Target = StringInterceptTargets.Output;

			StringInterceptor si = new StringInterceptor(attr, typeof(string));

			// string test
			string strValue = " test   ";

			strValue = (string)si.Intercept(strValue);

			// null test
			strValue = (string)si.Intercept(null);

			// int test
			si = new StringInterceptor(attr, typeof(int));

			int intValue = 100;

			intValue = (int)si.Intercept(100);

			// null test
			// intValue = (int)si.Intercept(null);

			// DateTime test
			si = new StringInterceptor(attr, typeof(DateTime));

			DateTime dtValue = (DateTime)si.Intercept(DateTime.Now);

			// Nullable test
			si = new StringInterceptor(attr, typeof(DateTime?));

			DateTime? dtValue2 = (DateTime?)si.Intercept(DateTime.Now);

			Dictionary<string, PayNotify> dict = new Dictionary<string, PayNotify>();
			PayNotify notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			PayNotify notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			dict.Add("test", notify);
			dict.Add("test2", notify2);

			Dictionary<string, string> dictStr = new Dictionary<string, string>();

			dictStr.Add("test", "  test   ");
			dictStr.Add("test2", " test   ");

			List<PayNotify> list = new List<PayNotify>();
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			list.Add(notify);
			list.Add(notify2);

			PayNotify[] arr = new PayNotify[2];
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			arr[0] = notify;
			arr[1] = notify2;

			// PayNotify Test
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.ExtandProperties = dict;
			notify.ExtandProperties2 = dictStr;
			notify.ExtandList = list;
			notify.ExtandArr = arr;

			si = new StringInterceptor(attr, typeof(PayNotify));

			si.Intercept(notify);




			// struct test
			TestStruct testStruct = new TestStruct();
			testStruct.Value = "  test  ";
			testStruct.I = 10;

			si = new StringInterceptor(attr, typeof(TestStruct));

			testStruct = (TestStruct)si.Intercept(testStruct);


			// ReturnValue Test
			si = new StringInterceptor(attr, typeof(XMS.Core.ReturnValue<PayNotify>));

			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";

			object value = si.Intercept(XMS.Core.ReturnValue<PayNotify>.Get200OK(notify));

			// dict test
			dict = new Dictionary<string, PayNotify>();
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			dict.Add("test", notify);
			dict.Add("test2", notify2);

			si = new StringInterceptor(attr, typeof(Dictionary<string, PayNotify>));

			si.Intercept(dict);

			// dict-string test
			dictStr = new Dictionary<string, string>();

			dictStr.Add("test", "  test   ");
			dictStr.Add("test2", " test   ");

			si = new StringInterceptor(attr, typeof(Dictionary<string, string>));

			si.Intercept(dictStr);


			// list test
			list = new List<PayNotify>();
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			list.Add(notify);
			list.Add(notify2);

			si = new StringInterceptor(attr, typeof(List<PayNotify>));

			si.Intercept(list);

			// 
			List<int> listInt = new List<int>();
			listInt.Add(10);
			listInt.Add(100);

			si = new StringInterceptor(attr, typeof(List<int>));

			si.Intercept(listInt);

			// 
			List<string> listString = new List<string>();
			listString.Add(" test   ");
			listString.Add("   test ");

			si = new StringInterceptor(attr, typeof(List<string>));

			si.Intercept(listString);

			// Array test
			arr = new PayNotify[2];
			notify = new PayNotify();
			notify.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify.ErrorMessage = "dddd<br/> \r\n ddd         ";
			notify.A = new A();

			notify2 = new PayNotify();
			notify2.PayPlatform = "aaaaaa<br/> \r\n aaa         ";
			notify2.RequestType = "bbbbbbbb<br/> \r\n bbb         ";
			notify2.PayOrderNo = "ccccc<br/> \r\n ccc         ";
			notify2.ErrorMessage = "dddd<br/> \r\n ddd         ";

			arr[0] = notify;
			arr[1] = notify2;

			si = new StringInterceptor(attr, typeof(PayNotify[]));

			si.Intercept(arr);

			// 
			int[] arrInt = new int[2];
			arrInt[0] = 10;
			arrInt[1] = 100;

			si = new StringInterceptor(attr, typeof(int[]));

			si.Intercept(arrInt);

			// 
			string[] arrString = new string[2];
			arrString[0] = " asdf  ";
			arrString[1] = "  test  ";

			si = new StringInterceptor(attr, typeof(string[]));
			si.Intercept(arrString);
		}
	}


	public class A
	{
		public string A1 = "        b1";

		private string a2 = "b2      ";
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

		private B b = new B();
		public B B
		{
			get
			{
				return b;
			}
		}

		public C C
		{
			get;
			set;
		}
	}

	public class B
	{
		public string B1 = "    b1";

		private string b2 = "           b2";
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

		public C C = new C();
	}

	public class C
	{
		public string C1 = "    c1 ";

		private string c2 = "        c2 ";

		public string C2
		{
			get
			{
				return this.c2;
			}
			set
			{
				this.c2 = value;
			}
		}
	}

	/// <summary>
	/// 验证汇付返回请求后的对象
	/// </summary>
	public sealed class PayNotify
	{
		public PayNotify()
		{
		}

		/// <summary>
		/// 支付平台
		/// </summary>
		[IgnoreStringIntercept]
		public string PayPlatform
		{
			get;
			set;
		}

		/// <summary>
		/// 支付订单号
		/// </summary>
		public string PayOrderNo
		{
			get;
			set;
		}

		/// <summary>
		/// 请求类型
		/// </summary>
		public string RequestType
		{
			get;
			set;
		}

		/// <summary>
		/// 支付平台处理请求的过程中是否出错
		/// </summary>
		public bool HasError
		{
			set;
			get;
		}

		/// <summary>
		/// 支付平台处理请求的过程中出错时的错误信息
		/// </summary>
		public string ErrorMessage
		{
			get;
			set;
		}

		[IgnoreStringIntercept]
		public Dictionary<string, PayNotify> ExtandProperties { get; set; }

		[StringIntercept(TrimSpace = false, AntiXSS = false, FilterSensitiveWords = false, WellFormatType = StringWellFormatType.Html, Target = StringInterceptTarget.Output)]
		[StringIntercept(TrimSpace = true, AntiXSS = true, FilterSensitiveWords = true, WellFormatType = StringWellFormatType.Text, Target = StringInterceptTarget.Input)]
		public Dictionary<string, string> ExtandProperties2 { get; set; }

		public List<PayNotify> ExtandList { get; set; }
		public PayNotify[] ExtandArr { get; set; }

		public A A
		{
			get;
			set;
		}

	}

	public struct TestStruct
	{
		private string value;

		public string Value
		{
			get
			{
				return this.value;
			}
			set
			{
				this.value = value;
			}
		}

		public int I
		{
			get;
			set;
		}
	}
}
