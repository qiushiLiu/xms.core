using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using System.Data;

using XMS.Core;
using XMS.Core.Web;

namespace XMS.Core.Test.Web
{
	[TestClass]
	public class JsonHelperTest
	{
		public JsonHelperTest()
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

		[TestMethod]
		public void Test()
		{
			System.Collections.Generic.Dictionary<string, string> dict = new System.Collections.Generic.Dictionary<string, string>();

			dict["aa"] = "1";
			dict["bb"] = "2";

			string s = JsonHelper.ConvertObjectToJsonString(dict);

			System.Collections.Generic.Dictionary<string, string> o = JsonHelper.ConvertJsonStringToObject<System.Collections.Generic.Dictionary<string, string>>(s);

			Assert.IsNotNull(o);
		}
	}
}
