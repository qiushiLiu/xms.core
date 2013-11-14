using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XMS.Core.Test
{
	[TestClass]
	public class IDictionaryServiceTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			XMS.Core.Dictionary.IDictionaryService dictService = XMS.Core.Container.Instance.Resolve<XMS.Core.Dictionary.IDictionaryService>();
			XMS.Core.Dictionary.Dictionary dict = dictService.GetDictionary(20000, "test");

			Assert.IsNotNull(dict);
		}
	}
}
