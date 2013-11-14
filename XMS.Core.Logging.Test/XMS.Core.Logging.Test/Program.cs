using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using XMS.Core;
using XMS.Core.Pay;

namespace XMS.Core.Logging.Test
{
	class Program
	{
		private static void Test3()
		{
			throw new BusinessException(100, "inner exception 3");
		}

		private static void Test2()
		{
			try
			{
				Test3();
			}
			catch (Exception err)
			{
				throw new ArgumentException("inner exception 2", err);
			}
		}

		private static void Test1()
		{
			try
			{
				Test2();
			}
			catch (Exception err)
			{
				throw new ContainerException("inner exception 1", err);
			}
		}

		private static Exception ThrowE()
		{
			try
			{
				Test2();
			}
			catch (Exception err)
			{
				throw new ContainerException("root exception", err);
			}
			throw new ContainerException("root exception");
		}


		static void Main(string[] args)
		{
			Thread thread = new Thread(new ThreadStart(Run));
			//设置为后台线程，这样将不会阻止进程终止
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.Lowest;
			thread.Start();


			while (true)
			{
				System.Threading.Thread.Sleep(1);
			}
		}

		private static IPNRService PNRService
		{
			get
			{
				return XMS.Core.Container.Instance.Resolve<IPNRService>();
			}
		}

		public static void Run()
		{
			int i = 0;
			while (true)
			{
				i++;

				XMS.Core.Container.LogService.Info("begin " + i + " 次循环");

				using (RunScope scope1 = RunScope.CreateRunContextScopeForRelease())
				{
					if (i < 100)
					{
						PNRService.CreateBuyUrl(i.ToString(), 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
					}
					else
					{
						using (AppAgentScope scope = AppAgentScope.CreateFromEnvironment())
						{
							PNRService.CreateBuyUrl(i.ToString(), 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
						}
					}
				}

				using (RunScope scope1 = RunScope.CreateRunContextScopeForDemo())
				{
					PNRService.CreateBuyUrl("demo" + i.ToString(), 100, "order", "1", "123456789", "", "CB", "http://www.57.cn", "http://www.57.cn");
				}

				System.Threading.Thread.Sleep(TimeSpan.FromMilliseconds(500));

				if (i % 5 == 0)
				{
					try
					{
						ThrowE();
					}
					catch (Exception err)
					{
						XMS.Core.Container.LogService.Error(err, "test");
					}
				}
			}
		}
	}
}
