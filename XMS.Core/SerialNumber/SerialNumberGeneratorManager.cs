using System;
using System.Collections.Generic;

using XMS.Core.Entity;

namespace XMS.Core.SerialNumber
{
	/* 建表
CREATE TABLE [dbo].[SerialNumberSeed](
	[Key] [nvarchar](30) NOT NULL,
	[CurrentValue] [int] NOT NULL,
	[TS] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Key] ASC
) ON [PRIMARY]
) ON [PRIMARY]
	 */
	/// <summary>
	/// 序列号生成器管理器。
	/// </summary>
	public abstract class SerialNumberGeneratorManager
	{
		private class SerialNumberBusinessContext : DbBusinessContextBase
		{
			public SerialNumberBusinessContext(string nameOrConnectionStringKey)
				: base(nameOrConnectionStringKey)
			{
			}

			private Dictionary<Type, string> modelMappings = null;

			public override Dictionary<Type, string> GetModelMappings()
			{
				if (this.modelMappings == null)
				{
					this.modelMappings = new Dictionary<Type, string>();

					this.modelMappings.Add(typeof(SerialNumberSeed), "SerialNumberSeed");
				}
				return this.modelMappings;
			}
		}

		/// <summary>
		/// 创建业务相关的上下文，以用于操作 SerialNumberSeed 表
		/// </summary>
		/// <returns></returns>
		internal protected virtual IBusinessContext CreateBusinessContext(string nameOrConnectionString)
		{
			return new SerialNumberBusinessContext(nameOrConnectionString);
		}

		/// <summary>
		/// </summary>
		/// <param name="generatorKey"></param>
		/// <returns></returns>
		protected abstract ISerialNumberGenerator CreateSerialNumberGenerator(string generatorKey);

		#region 获取序列号生成器
		private Dictionary<string, ISerialNumberGenerator> generators = new Dictionary<string, ISerialNumberGenerator>();
		private Dictionary<string, ISerialNumberGenerator> generators_Demo = new Dictionary<string, ISerialNumberGenerator>();

		private object syncForGenerators = new object();

		/// <summary>
		/// 获取指定键值的序列号生成器。
		/// </summary>
		/// <param name="generatorKey">要获取的序列号生成器的键。</param>
		/// <returns>获取到得序列号生成器。</returns>
		public ISerialNumberGenerator GetSerialNumberGenerator(string generatorKey)
		{
			if (String.IsNullOrEmpty(generatorKey))
			{
				throw new ArgumentNullException("generatorKey");
			}

			ISerialNumberGenerator generator;

			lock (syncForGenerators)
			{
				if (RunContext.Current.RunMode == RunMode.Release)
				{
					if (!this.generators.TryGetValue(generatorKey, out generator))
					{
						generator = this.CreateSerialNumberGenerator(generatorKey);

						this.generators.Add(generatorKey, generator);
					}
				}
				else
				{
					if (!this.generators_Demo.TryGetValue(generatorKey, out generator))
					{
						generator = this.CreateSerialNumberGenerator(generatorKey);

						this.generators_Demo.Add(generatorKey, generator);
					}
				}
			}

			return generator;
		}
		#endregion
	}
}