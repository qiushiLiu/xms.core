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
    public class AppAgentTest
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

		private IPNRService PNRService
		{
			get
			{
				//if (this.pnrServiceFactory != null)
				//{
				//    return this.pnrServiceFactory.CreateService();
				//}

				return XMS.Core.Container.Instance.Resolve<IPNRService>();
			}
		}

		private void Test3()
		{
			throw new BusinessException(100, "手机号码不能为空");
		}


		private void Test2()
		{
			try
			{
				Test3();
			}
			catch (Exception err)
			{
				throw new ArgumentException("参数错误", err);
			}
		}

		private void Test1()
		{
			try
			{
				Test2();
			}
			catch (Exception err)
			{
				throw new ContainerException("初始化过程中发生错误", err);
			}
		}

		private Exception ThrowE()
		{
			try
			{
				Test2();
			}
			catch (Exception err)
			{
				return new ContainerException("初始化过程中发生错误", err);
			}
			return new ContainerException("初始化过程中发生错误");
		}

        /// <summary>
        ///A test for ToHtmlEncode
        ///</summary>
        [TestMethod()]
        public void ServiceTest()
        {
			while (true)
			{
				ReturnValue<string> retValue = XMS.Core.Container.Instance.Resolve<XMS.Core.Pay.IPNRService>().CreateBuyUrl("a" + "_1"
								 , 100, "A", "1", "13800138000", null, "AN", "http://www.xiaomishu.com", "http://www.xiaomishu.com");
				//ReturnValue<string> retValue = this.PNRService.CreateBuyUrl("123default<br/>aaa\r\nbbb            ", 100, "       order", "        1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");

				if (retValue.Code == 200)
				{

					ReturnValue<XMS.Core.Pay.PayNotify> retPayNotify = this.PNRService.BuyPayOut("123default<br/>aaa\r\nbbb            ", 100, "       order", "        1", "123456789", "", "CB", "http://www.57.cn");

					if (retPayNotify.Code == 201)
					{
						break;
					}
				}
				System.Threading.Thread.Sleep(1000);
			}

			char[] chars = new char[5000000];
			for (int i = 0; i < chars.Length; i++)
			{
				chars[i] = (i % 10).ToString()[0];
			}

			this.PNRService.CreateBuyUrl(new string(chars), 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");

			try
			{
				Test1();
			}
			catch (Exception err)
			{
				XMS.Core.Container.LogService.Fatal(err, "test");
			}

			for (int i = 0; i < 100; i++)
			{
				using (AppAgentScope scope = AppAgentScope.CreateFromEnvironment())
				{
					this.PNRService.CreateBuyUrl("123default", 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
				}

				using (RunScope scope1 = RunScope.CreateRunContextScopeForRelease())
				{
					this.PNRService.CreateBuyUrl("123release", 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
				}

				using (RunScope scope1 = RunScope.CreateRunContextScopeForDemo())
				{
					ReturnValue<string> retValue = this.PNRService.CreateBuyUrl("123demo", 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
					if (retValue.Code == 200)
					{
						if (i > 98)
						{
							i++;
						}
					}
				}

				if (i > 40)
				{
					XMS.Core.Container.LogService.Warn(new BusinessException("业务出错了" + " warn test" + i.ToString() +( i>80 ? "test1" : "test2")));

					XMS.Core.Container.LogService.Warn(new BusinessException("业务出错了" + " warn test" + i.ToString() + (i > 80 ? "test1" : "test2"), ThrowE()));

					if (i > 60)
					{
						using (RunScope scope1 = RunScope.CreateRunContextScopeForDemo())
						{
							XMS.Core.Container.LogService.Error("error test" + i.ToString() + " for demo", "test");
						}
					}
				}
			}
        }
    }
}
