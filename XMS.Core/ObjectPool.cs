using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace XMS.Core
{
	/// <summary>
	/// 定义一组可用来操纵对象的方法。
	/// </summary>
	/// <typeparam name="T">池中存放的对象的类型。</typeparam>
	public interface IPool<T> : IDisposable where T : class
	{
		/// <summary>
		/// 从对象池中获取一个可用的对象以进行操作， 对象使用完毕后，请调用 <see cref="Push"/> 方法将其放回对象池，对象使用过程中如果发现已失效，请调用 <see cref="Release"/> 方法释放它。
		/// </summary>
		/// <returns>从对象池中取到的对象。</returns>
		T Pop();

		/// <summary>
		/// 将从对象池中获取的对象重新放入对象池。
		/// </summary>
		/// <param name="item">要放入对象池中的对象。</param>
		void Push(T item);

		/// <summary>
		///	释放从对象池中获取的对象。
		/// </summary>
		/// <param name="item">要释放的对象。</param>
		void Release(T item);
	}

	/*
	设计目标：
	  1.复用构造出来的对象
	  2.避免重复创建和销毁对象对GC造成的压力
	  3.避免重复创建对象造成的资源消耗

	适用场景:
	  构造对象过程比较复杂，对象消耗资源较多

	特性：
	  线程安全，使用Lock Free的实现方式，支持高并发、高吞吐量；
	  支持设置上下限；
	  除了提供 Pop、Push 方法用于获取、放回对象外，还提供 Execute 方法用于自动从池中取可用对象并以参数形式传入并执行目标方法，方法执行完后自动将对象重新放回线程池；
	*/
	/// <summary>
	/// 提供一个通用对象池模式的轻量实现。
	/// </summary>
	/// <typeparam name="T">池中存放的对象的类型。</typeparam>
	public class ObjectPool<T> : IPool<T> where T : class
	{
		// 实际存放在对象池中的对象
		private class PooledItem
		{
			public T Value;

			public DateTime PushTime;

			public PooledItem(T value, DateTime pushTime)
			{
				this.Value = value;
				this.PushTime = pushTime;
			}
		}

		#region 基本参数
		// 对象池的名称，同时使用此名称为对象池创建性能计数器实例的名称
		private string name;

		// 对象创建函数
		private Func<T> createFunction = null;

		// 资源控制
		// 0 <= lowWatermark <= highWatermark <= maxCount
		// 对象池低水位线；
		private int lowWatermark = 0;

		// 对象池高水位线
		private int highWatermark = 0;

		// 由对象池当前维护的有效对象的最大数量。
		private int maxCount = 0;

		// 超时控制
		// 非活动超时时间，对象池中任何一个对象，如果在该参数设定的时间周期内未被使用，那么该对象将被抛弃并释放。
		private TimeSpan inActiveTimeout = TimeSpan.FromMinutes(2);
		#endregion

		#region 性能监测
		// 当前有效数量性能监视器
		private PerformanceCounter countPerfCounter = null;
		// 当前空闲数量性能监视器
		private PerformanceCounter idleCountPerfCounter = null;

		private bool lastEnablePerfCounters = false;

		private object lock4perfCounters = new object();
		/// <summary>
		/// 获取或设置一个值，该值指示当前对象池是否启用性能计数器，如果启用则为 true， 否则为 false。
		/// </summary>
		/// <remarks>
		/// 该值可在运行时修改，以根据需要观察性能。当在程序运行时修改 ObjectPoolPerfCounters 参数启用性能计数器时，要新开性能计数器监测面板以查看结果，在原性能计数器监测面板可能要延迟几分钟才能看到结果。
		/// </remarks>
		protected bool EnablePerfCounters
		{
			get
			{
				bool enablePerfCounters = Container.ConfigService.GetAppSetting<string>("ObjectPoolPerfCounters", Empty<string>.HashSet).Contains(this.name);

				// 在启用性能监测参数由不启用变为启用时初始化性能计数器实例，反之，释放现有性能计数器实例
				if(enablePerfCounters != lastEnablePerfCounters)
				{
					lock (lock4perfCounters)
					{
						if (enablePerfCounters)
						{
							if (this.idleCountPerfCounter == null)
							{
								this.idleCountPerfCounter = ObjectPoolPerformanceCounterManager.Instance.CreateInstance(ObjectPoolPerformanceCounterManager.ObjectPool_IdleCount, this.name.Replace('/', '|'));
								if (this.idleCountPerfCounter != null)
								{
									this.idleCountPerfCounter.RawValue = this.idleCount;
								}
							}
							if (this.countPerfCounter == null)
							{
								this.countPerfCounter = ObjectPoolPerformanceCounterManager.Instance.CreateInstance(ObjectPoolPerformanceCounterManager.ObjectPool_Count, this.name.Replace('/', '|'));
								if (this.countPerfCounter != null)
								{
									this.countPerfCounter.RawValue = this.count;
								}
							}
						}
						else
						{
							if (this.countPerfCounter != null)
							{
								this.countPerfCounter.Dispose();
								this.countPerfCounter.RemoveInstance();
								this.countPerfCounter = null;

							}
							if (this.idleCountPerfCounter != null)
							{
								this.idleCountPerfCounter.Dispose();
								this.idleCountPerfCounter.RemoveInstance();
								this.idleCountPerfCounter = null;
							}
						}
					}

					this.lastEnablePerfCounters = enablePerfCounters;
				}
				return enablePerfCounters;
			}
		}
		#endregion

		#region 内部变量
		// 对象池低水位对象集合，低水位区的对象采用 FIFO（先进先出）算法
		private Queue<PooledItem> queue = new Queue<PooledItem>();
		// 对象池高水位对象集合，高水位区的对象采用 LIFO（后进先出）算法
		private LinkedList<PooledItem> list = new LinkedList<PooledItem>();

		private int idleCount = 0;	// 空闲对象的数量
		private int count = 0;		// 池中生成对象的数量

		// 清理线程
		private Thread cleanThread = null;
		#endregion

		#region Lock-Free 锁定机制
		// 标识锁定状态
		private int isLocked = 0;

		/// <summary>
		/// 进入锁定状态。
		/// </summary>
		/// <param name="millisecondsTimeout">超时毫秒数，-1 或其它小于 0 的值，表示成功进入锁定状态前永不超时， 0 或其它大于 0 的值，表示成功进入锁定状态前的最大等待时间。</param>
		protected void EnterLock(int millisecondsTimeout)
		{
			long startTicks = 0;

			if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
			{
				startTicks = DateTime.UtcNow.Ticks;
			}

			SpinWait wait = new SpinWait();
			do
			{
				if (Interlocked.CompareExchange(ref isLocked, 1, 0) == 0)
				{
					return;
				}

				wait.SpinOnce();
			}
			while (millisecondsTimeout != 0 && (millisecondsTimeout <= -1 || !wait.NextSpinWillYield || !TimeoutExpired(startTicks, millisecondsTimeout)));

			throw new TimeoutException(String.Format("在指定的超时时间 {0} ms 内未能进入锁定状态", millisecondsTimeout));
		}

		/// <summary>
		/// 退出锁定状态。
		/// </summary>
		protected void ExitLock()
		{
			Thread.VolatileWrite(ref isLocked, 0);
		}

		private static bool TimeoutExpired(long startTicks, int originalWaitTime)
		{
			return DateTime.UtcNow.Ticks - startTicks >= (originalWaitTime * 10000);
		}
		#endregion

		/// <summary>
		/// 获取当前对象池中有效对象的数量。
		/// </summary>
		public int Count
		{
			get
			{
				return this.count;
			}
		}

		/// <summary>
		/// 获取对象池中空闲对象的数量。
		/// </summary>
		public int IdleCount
		{
			get
			{
				return this.idleCount;
			}
		}

        public int BusyCount
        {
            get
            {
                return this.count - this.idleCount;
            }
        }

		/// <summary>
		/// 初始化 ObjectPool 类的新实例。
		/// </summary>
		/// <param name="name">对象池的名称，同时也使用此名称创建性能计数器实例。</param>
		/// <param name="createFunction">用来初始化对象的函数</param>
		/// <param name="lowWatermark">对象池低水位线，Pop 时，lowWatermark 以下的对象按 FIFO（先进先出）算法进行提取，以平均使用每个对象并维持池中对象为教合适的状态。</param>
		/// <param name="highWatermark">对象池高水位线，Pop 时，lowWatermark 和 highWatermark之间的对象按 LIFO（后进先出）算法进行提取，以尽可能使用最近经常使用的对象。</param>
		/// <param name="maxCount">对象池中生成的可用对象数量的最大值</param>
		/// <param name="initSize">对象池下限</param>
		/// <param name="inActiveTimeout">对象非活动逐出超时时间，如果该值小于等于 TimeSpan.Zero， 则对象永不超时逐出。</param>
		public ObjectPool(string name, Func<T> createFunction, int lowWatermark, int highWatermark, int maxCount, int initSize, TimeSpan inActiveTimeout)
		{
			if (String.IsNullOrEmpty(name))
			{
				throw new ArgumentNullOrEmptyException("name");
			}
			if (createFunction == null)
			{
				throw new ArgumentNullException("createFunction");
			}
			if (lowWatermark < 0)
			{
				throw new ArgumentOutOfRangeException("lowWatermark");
			}
			if (highWatermark <= 0)
			{
				throw new ArgumentOutOfRangeException("highWatermark");
			}
			if (maxCount <= 0)
			{
				throw new ArgumentOutOfRangeException("maxCount");
			}
			if (highWatermark < lowWatermark)
			{
				throw new ArgumentException("highWatermark 必须大于等于 lowWatermark。");
			}
			if (maxCount < highWatermark)
			{
				throw new ArgumentException("maxCount 必须大于等于 highWatermark。");
			}
			if (initSize < 0)
			{
				throw new ArgumentOutOfRangeException("initSize");
			}

			this.name = name;

			this.createFunction = createFunction;

			this.lowWatermark = lowWatermark;
			this.highWatermark = highWatermark;

			this.maxCount = maxCount;

			for (int i = 0; i < initSize; i++)
			{
				if (i < this.lowWatermark)
				{
					this.queue.Enqueue(new PooledItem(createFunction(), DateTime.Now));
				}
				else if (i < this.highWatermark)
				{
					this.list.AddFirst(new PooledItem(createFunction(), DateTime.Now));
				}
				else
				{
					break;
				}
			}

			this.idleCount = initSize;
			this.count = initSize;

			this.inActiveTimeout = inActiveTimeout <= TimeSpan.Zero ? TimeSpan.MaxValue : inActiveTimeout;
		}

		#region Execute
		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		public void Execute(Action<T> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		public void Execute<T2>(Action<T, T2> action, T2 t2)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item, t2);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		public void Execute<T2, T3>(Action<T, T2, T3> action, T2 t2, T3 t3)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item, t2, t3);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		public void Execute<T2, T3, T4>(Action<T, T2, T3, T4> action, T2 t2, T3 t3, T4 t4)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item, t2, t3, t4);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		/// <param name="t5">action 的第四个参数</param>
		public void Execute<T2, T3, T4, T5>(Action<T, T2, T3, T4, T5> action, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item, t2, t3, t4, t5);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		/// <param name="t5">action 的第四个参数</param>
		/// <param name="t6">action 的第四个参数</param>
		public void Execute<T2, T3, T4, T5, T6>(Action<T, T2, T3, T4, T5, T6> action, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						action(item, t2, t3, t4, t5, t6);

						break;
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}
	
		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		public TResult Execute<TResult>(Func<T, TResult> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		public TResult Execute<T2, TResult>(Func<T, T2, TResult> action, T2 t2)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item, t2);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		public TResult Execute<T2, T3, TResult>(Func<T, T2, T3, TResult> action, T2 t2, T3 t3)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item, t2, t3);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		public TResult Execute<T2, T3, T4, TResult>(Func<T, T2, T3, T4, TResult> action, T2 t2, T3 t3, T4 t4)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item, t2, t3, t4);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		/// <param name="t5">action 的第四个参数</param>
		public TResult Execute<T2, T3, T4, T5, TResult>(Func<T, T2, T3, T4, T5, TResult> action, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item, t2, t3, t4, t5);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}

		/// <summary>
		/// 从对象池中取一个可用对象出来, 并执行指定的方法, 执行完成以后将对象重新放回池中。
		/// </summary>
		/// <param name="action">一个可用的对象</param>
		/// <param name="t2">action 的第二个参数</param>
		/// <param name="t3">action 的第三个参数</param>
		/// <param name="t4">action 的第四个参数</param>
		/// <param name="t5">action 的第四个参数</param>
		/// <param name="t6">action 的第四个参数</param>
		public TResult Execute<T2, T3, T4, T5, T6, TResult>(Func<T, T2, T3, T4, T5, T6, TResult> action, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			T item = null;
			try
			{
				while (true)
				{
					item = this.Pop();

					try
					{
						return action(item, t2, t3, t4, t5, t6);
					}
					catch (ObjectDisposedException)
					{
						this.Release(item);

						item = null;

						continue;
					}
					catch
					{
						throw;
					}
				}
			}
			finally
			{
				if (item != null)
				{
					this.Push(item);
				}
			}
		}
		#endregion

		/// <summary>
		/// 从对象池中获取一个可用的对象以进行操作， 对象使用完毕后，请调用 <see cref="Push"/> 方法将其放回对象池，对象使用过程中如果发现已失效，请调用 <see cref="Release"/> 方法释放它。
		/// </summary>
		/// <returns>从对象池中取到的对象。</returns>
		public T Pop()
		{
			return this.Pop(-1);
		}

		/// <summary>
		/// 从对象池中获取一个可用的对象以进行操作， 对象使用完毕后，请调用 Push 方法将其放回对象池。
		/// </summary>
		/// <param name="millisecondsTimeout">超时毫秒数，0 立即返回，负值表示永不过期。</param>
		/// <returns>从对象池中取到的对象。</returns>
		public T Pop(int millisecondsTimeout)
		{

			SpinWait wait = new SpinWait();

			long startTicks = 0;

			if (millisecondsTimeout != -1 && millisecondsTimeout != 0)
			{
				startTicks = DateTime.UtcNow.Ticks;
			}

			T item = default(T);

			TimeoutHelper helper = new TimeoutHelper(millisecondsTimeout < 0 ? TimeSpan.MaxValue : TimeSpan.FromMilliseconds(millisecondsTimeout));

			while (true)
			{
				if (this.disposed)
				{
					throw new ObjectDisposedException("ObjectPool");
				}

				if (Interlocked.Decrement(ref this.idleCount) >= 0)
				{
					this.EnterLock((int)helper.RemainingTime().TotalMilliseconds);

					try
					{
						PooledItem pooledItem = null;

						if (this.queue.Count > 0)
						{
							pooledItem = this.queue.Dequeue();
						}
						else
						{
							pooledItem = this.list.First.Value;

							this.list.RemoveFirst();
						}

						if (this.EnablePerfCounters)
						{
							if (this.idleCountPerfCounter != null)
							{
								try
								{
									this.idleCountPerfCounter.Decrement();
								}
								catch { }
							}
						}

						if(DateTime.Now - pooledItem.PushTime > this.inActiveTimeout)
						{
							this.Release(pooledItem.Value);

							continue;
						}
						else
						{
							item = pooledItem.Value;
						}

						// 另起线程清理
						if (this.cleanThread != null)
						{
							if (this.list.Count > 0 &&  DateTime.Now - this.list.Last.Value.PushTime > this.inActiveTimeout)
							{
								this.cleanThread = new Thread(this.Clean);

								this.cleanThread.Start();
							}
						}

						break;
					}
					catch
					{
						// 出现任何错误时都将 idleCount 加回
						Interlocked.Increment(ref this.idleCount);

						throw;
					}
					finally
					{
						this.ExitLock();
					}
				}
				else
				{
					// 把 idleCount 加回
					Interlocked.Increment(ref this.idleCount);

					if (Interlocked.Increment(ref this.count) <= this.maxCount)
					{
						try
						{
							item = createFunction();

							if (this.EnablePerfCounters)
							{
								if (this.countPerfCounter != null)
								{
									try
									{
										this.countPerfCounter.Increment();
									}
									catch { }
								}
							}

							break;
						}
						catch
						{
							// 出现任何错误时都将 count 减回
							Interlocked.Decrement(ref this.count);

							throw;
						}
					}
					else
					{
						// 把 count 减回
						Interlocked.Decrement(ref this.count);
					}

					// 执行到这里，说明未找到可用空闲对象，且已满

					// 等待一个短暂间隔后继续
					wait.SpinOnce();

					if (millisecondsTimeout != 0 && (millisecondsTimeout <= -1 || !wait.NextSpinWillYield || !TimeoutExpired(startTicks, millisecondsTimeout)))
					{
						continue;
					}
					else
					{
						throw new TimeoutException(String.Format("在 {0} ms 超时时间内未能从池中取到可用的对象。", millisecondsTimeout));
					}
				}
			}

			return item;
		}

		private void Clean()
		{
			try
			{
				while (this.list.Count > 0)
				{
					if (Interlocked.Decrement(ref this.idleCount) >= 0)
					{
						this.EnterLock(1000);

						try
						{
							PooledItem pooledItem = null;

							if (this.list.Last != null && DateTime.Now - this.list.Last.Value.PushTime > this.inActiveTimeout)
							{
								pooledItem = this.list.Last.Value;

								this.list.RemoveLast();
							}

							if (pooledItem != null)
							{
								this.Release(pooledItem.Value);

								if (this.EnablePerfCounters)
								{
									if (this.idleCountPerfCounter != null)
									{
										try
										{
											this.idleCountPerfCounter.Decrement();
										}
										catch { }
									}
								}

								continue;
							}
							else
							{
								// 把 idleCount 加回
								Interlocked.Increment(ref this.idleCount);

								break;
							}
						}
						catch
						{
							// 出现任何错误时都将 idleCount 加回
							Interlocked.Increment(ref this.idleCount);

							throw;
						}
						finally
						{
							this.ExitLock();
						}
					}
					else
					{
						// 把 idleCount 加回
						Interlocked.Increment(ref this.idleCount);

						break;
					}
				}
			}
			catch(Exception err)
			{
				XMS.Core.Container.LogService.Warn(String.Format("清理对象池 {0} 中过期对象的过程中发生错误", this.name), err); 
			}
			finally
			{
				this.cleanThread = null;
			}
		}

		/// <summary>
		/// 将从对象池中获取的对象重新放入对象池。
		/// </summary>
		/// <param name="item">要放入对象池中的对象。</param>
		public void Push(T item)
		{
			this.Push(item, -1);
		}

		/// <summary>
		/// 将从对象池中获取的对象重新放入对象池。
		/// </summary>
		/// <param name="item">要放入对象池中的对象。</param>
		/// <param name="millisecondsTimeout">超时毫秒数，0 立即返回，负值表示永不超时。</param>
		public void Push(T item, int millisecondsTimeout)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}

			if (this.disposed)
			{
				// 认为要 push 的 item 是当前线程池创建的，当线程池已释放时，不抛出异常，也不将对象放入池中，而是将对象释放
				this.Release(item);

				return;
			}

			if (this.idleCount < this.highWatermark)
			{
				try
				{
					this.EnterLock(millisecondsTimeout);
				}
				catch (Exception err)
				{
					if (err is TimeoutException)
					{
						// 注意：超时时，仅将对象释放，但不抛出异常
						this.Release(item);
					}

					throw;
				}

				try
				{
					if (this.idleCount < this.highWatermark)
					{
						if (this.queue.Count < this.lowWatermark)
						{
							this.queue.Enqueue(new PooledItem(item, DateTime.Now));
						}
						else
						{
							this.list.AddFirst(new PooledItem(item, DateTime.Now));
						}

						Interlocked.Increment(ref this.idleCount);

						if (this.EnablePerfCounters)
						{
							if (this.idleCountPerfCounter != null)
							{
								try
								{
									this.idleCountPerfCounter.Increment();
								}
								catch { }
							}
						}
					}
					else
					{
						// 超过 highWatermark 时，应释放项
						this.Release(item);
					}
				}
				finally
				{
					this.ExitLock();
				}
			}
			else
			{
				// 超过 highWatermark 时，应释放项
				this.Release(item);
			}
		}

		/// <summary>
		///	释放从对象池中获取的对象。
		/// </summary>
		/// <param name="item">要释放的对象。</param>
		public void Release(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}

			Interlocked.Decrement(ref this.count);

			if (this.EnablePerfCounters)
			{
				if (this.countPerfCounter != null)
				{
					try
					{
						this.countPerfCounter.Decrement();
					}
					catch { }
				}
			}

			this.DisposeItem(item);
		}

		private void DisposeItem(T item)
		{
			if (item is IDisposable)
			{
				((IDisposable)item).Dispose();
			}
		}

		#region IDisposable interface
		private bool disposed = false;

		/// <summary>
		/// 释放托管和非托管资源。
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					this.EnterLock(-1);

					PooledItem[] all = this.queue.ToArray();

					for (int i = 0; i < all.Length; i++)
					{
						this.DisposeItem(all[i].Value);
					}

					all = this.list.ToArray();

					for (int i = 0; i < all.Length; i++)
					{
						this.DisposeItem(all[i].Value);
					}

					this.disposed = true;

					if (this.countPerfCounter != null)
					{
						this.countPerfCounter.Dispose();
					}
					if (this.idleCountPerfCounter != null)
					{
						this.idleCountPerfCounter.Dispose();
					}

					this.ExitLock();
				}

				this.disposed = true;
			}
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~ObjectPool()
		{
			Dispose(false);
		}
		#endregion
	}

	internal sealed class ObjectPoolPerformanceCounterManager : IDisposable
	{
		internal static string ObjectPool_Count = "ObjectPool_Count";
		internal static string ObjectPool_IdleCount = "ObjectPool_IdleCount";

		public static ObjectPoolPerformanceCounterManager Instance = new ObjectPoolPerformanceCounterManager();

		private string category = String.Format("{0}_{1}", RunContext.AppName, RunContext.AppVersion);

		private CounterCreationDataCollection counterCreationDatas = new CounterCreationDataCollection(
				new CounterCreationData[]{
					new CounterCreationData(ObjectPool_Count,		"对象池中可用对象数量", PerformanceCounterType.NumberOfItems32),
					new CounterCreationData(ObjectPool_IdleCount, "对象池中空闲对象数量", PerformanceCounterType.NumberOfItems32)
				}
			);

		public ObjectPoolPerformanceCounterManager()
		{
			// 注意：创建性能计数器类别是比较耗时的操作（要 3 秒左右），这有可能造成程序刚启动时出现性能问题，因此性能计数器的类别必须由外部工具创建和删除
			//if (PerformanceCounterCategory.Exists(this.category))
			//{
			//    PerformanceCounterCategory.Delete(this.category);
			//}

			//PerformanceCounterCategory.Create(this.category,			//类型名称
			//    this.category + " 性能测试",							//类型描述
			//    PerformanceCounterCategoryType.MultiInstance,	//类型的实例种类
			//    counterCreationDatas);							//创建性能计数器数据
		}

		// 注意：创建性能计数器实例是比较耗时的操作，超过 1 秒
		public PerformanceCounter CreateInstance(string counterName, string instanceName)
		{
			if (PerformanceCounterCategory.Exists(this.category) && PerformanceCounterCategory.CounterExists(counterName, this.category))
			{
				PerformanceCounter counter = new PerformanceCounter(this.category, counterName, instanceName, false);

				counter.RawValue = 0;

				return counter;
			}

			return null;
		}

		#region IDisposable interface
		private bool disposed = false;

		public void Dispose()
		{
			this.CheckAndDispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void CheckAndDispose(bool disposing)
		{
			if (!this.disposed)
			{
				this.Dispose(disposing);
			}
			this.disposed = true;
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			// 释放托管资源代码
			if (disposing)
			{
				//try
				//{
				//    if (PerformanceCounterCategory.Exists(category))
				//    {
				//        PerformanceCounterCategory.Delete(category);
				//    }
				//}
				//catch { }
			}
			// 释放非托管资源代码
		}

		~ObjectPoolPerformanceCounterManager()
		{
			this.CheckAndDispose(false);
		}
		#endregion

	}
}
