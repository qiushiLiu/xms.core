using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XMS.Core.Test
{
	public class TestTask : TriggerTaskBase
	{
		private DateTime? nextExecuteTime;
		int i = 1;

		public TestTask(string key, string name, DateTime? nextExecuteTime)
			: base(key, name)
		{
			this.nextExecuteTime = nextExecuteTime;
		}

		public override void Execute(DateTime? lastExecuteTime)
		{
			// this.LogService.Info("test" + i.ToString());

			if (i >= 10)
			{
				this.nextExecuteTime = null;
			}
			else
			{
				this.nextExecuteTime = lastExecuteTime == null ? DateTime.Now.AddSeconds(2) : lastExecuteTime.Value.AddSeconds(2);

				i++;
			}

			TriggerTaskHost.Instance.Count++;
		}

		public override DateTime? GetNextExecuteTime()
		{
			return nextExecuteTime;
		}
	}

	public sealed class TriggerTaskHost : TriggerTaskHostBase
	{
		public int Count = 0;

		private static TriggerTaskHost instance = new TriggerTaskHost("触发性任务宿主调度", TimeSpan.FromSeconds(1), System.Threading.ThreadPriority.BelowNormal);

		public static TriggerTaskHost Instance
		{
			get
			{
				return instance;
			}
		}

		private TriggerTaskHost(string name, TimeSpan flushInterval, System.Threading.ThreadPriority priority)
			: base(name, flushInterval, priority)
		{
		}


		protected override ITriggerTask[] CreateTriggerTasks()
		{
			DateTime now = DateTime.Now.AddSeconds(2);

			Dictionary<string, ITriggerTask> list = new Dictionary<string, ITriggerTask>();

			for (int i = 0; i < 1000; i++)
			{
				list.Add("test" + i.ToString(), new TestTask("test" + i.ToString(), "test" + i.ToString(), now));
			}

			return list.Values.ToArray();
		}
	}

	/// <summary>
	/// TaskTest 的摘要说明
	/// </summary>
	[TestClass]
	public class TaskTest
	{
		public TaskTest()
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
			TriggerTaskHost.Instance.Start();

			System.Threading.Thread.Sleep(40000);

			Assert.IsTrue(TriggerTaskHost.Instance.Count >= 10000);
		}
	}
}
