using System;
using System.Collections.Generic;

using XMS.Core;
using XMS.Core.Caching;
using XMS.Core.Entity;

namespace XMS.Core.SerialNumber
{
	/// <summary>
	/// 默认序列号生成器。
	/// </summary>
	public class DefaultSerialNumberGenerator : ISerialNumberGenerator
	{
		private SerialNumberGeneratorManager manager;

		private string nameOrConnectionString;

		private string generatorKey;
		private long seedInitialValue;
		private int step;
		private int poolSize;

		private Queue<long> currentPool;

		/// <summary>
		/// 获取序列号生成器的键。
		/// </summary>
		public string GeneratorKey
		{
			get
			{
				return this.generatorKey;
			}
		}

		/// <summary>
		/// 获取序列号池的大小。
		/// </summary>
		public int PoolSize
		{
			get
			{
				return this.poolSize;
			}
		}

		/// <summary>
		/// 使用指定的序列号生成器管理器、连接名称或连接字符串、生成器的键、种子初始值、步长、池大小创建序列号生成器。
		/// </summary>
		/// <param name="manager">序列号生成器管理器。</param>
		/// <param name="nameOrConnectionString">连接名称或连接字符串。</param>
		/// <param name="generatorKey">生成器的键。</param>
		/// <param name="seedInitialValue">种子初始值。</param>
		/// <param name="step">步长。</param>
		/// <param name="poolSize">池大小。</param>
		public DefaultSerialNumberGenerator(SerialNumberGeneratorManager manager, string nameOrConnectionString, string generatorKey, long seedInitialValue, int step, int poolSize)
		{
			if (manager == null)
			{
				throw new ArgumentNullException("manager");
			}

			if (String.IsNullOrWhiteSpace(nameOrConnectionString))
			{
				throw new ArgumentNullOrWhiteSpaceException("nameOrConnectionString");
			}

			if (String.IsNullOrWhiteSpace(generatorKey))
			{
				throw new ArgumentNullOrWhiteSpaceException("generatorKey");
			}

			if (seedInitialValue < 0)
			{
				throw new ArgumentOutOfRangeException("seedInitialValue");
			}

			if (step < 1)
			{
				throw new ArgumentOutOfRangeException("step");
			}

			if (poolSize < 0)
			{
				throw new ArgumentOutOfRangeException("poolSize");
			}
			this.manager = manager;

			this.nameOrConnectionString = nameOrConnectionString;

			this.generatorKey = generatorKey;
			this.seedInitialValue = seedInitialValue;
			this.step = step;
			this.poolSize = poolSize;

			this.currentPool = new Queue<long>(this.poolSize);
		}

		private object syncForEnqueue = new object();

		private object syncForDequeue = new object();

		private IBusinessContext CreateBusinessContext()
		{
			return this.manager.CreateBusinessContext(this.nameOrConnectionString);
		}

		/// <summary>
		/// 获取序列号。
		/// </summary>
		/// <returns>已格式化的序列号。</returns>
		public ISerialNumber GetSerialNumber()
		{
			while (true)
			{
				Queue<long> pool = this.currentPool;
				// 双重检查锁定
				if (pool.Count == 0)
				{
					lock (syncForEnqueue)
					{
						pool = this.currentPool;

						if (pool.Count == 0)
						{
							IBusinessContext businessContext = this.CreateBusinessContext();

							long currentValue = this.seedInitialValue;

							bool retrying = false;
							while (true)
							{
								using (IEntityContext entityContext = businessContext.CreateEntityContext())
								{
									try
									{
										SerialNumberSeed serialNumberSeed = entityContext.FindByPrimaryKey<SerialNumberSeed>(this.generatorKey);
										if (serialNumberSeed == null)
										{
											serialNumberSeed = new SerialNumberSeed();
											serialNumberSeed.Key = this.generatorKey;
											serialNumberSeed.CurrentValue = this.seedInitialValue + (this.poolSize * this.step);

											entityContext.Add<SerialNumberSeed>(serialNumberSeed);
										}
										else
										{
											currentValue = serialNumberSeed.CurrentValue;
											serialNumberSeed.CurrentValue = serialNumberSeed.CurrentValue + (this.poolSize * this.step);

											entityContext.Update<SerialNumberSeed>(serialNumberSeed);
										}
										// 未出错时直接跳出
										break;
									}
									catch (Exception err)
									{
										// 并发错误继续执行
										if (err is System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
										{
											continue;
										}

										// 非并发错误，支持重试一次
										if (!retrying)
										{
											retrying = true;
											continue;
										}
										else
										{
											throw;
										}
									}
								}
							}

							Queue<long> newQueue = new Queue<long>(this.poolSize);

							for (int i = 0; i < this.poolSize; i++)
							{
								newQueue.Enqueue(currentValue + 1 + (i * this.step));
							}

							pool = newQueue;
							this.currentPool = newQueue;
						}
					}
				}

				lock (syncForDequeue)
				{
					if (pool.Count > 0)
					{
						return this.CreateSerialNumber(pool.Dequeue());
					}
				}
			}
		}

		/// <summary>
		/// 创建序列号。
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		protected virtual ISerialNumber CreateSerialNumber(long number)
		{
			return new DefaultSerialNumber(number);
		}
	}
}