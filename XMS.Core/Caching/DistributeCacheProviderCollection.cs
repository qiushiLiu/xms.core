using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Configuration.Provider;

namespace XMS.Core.Caching
{
	internal class DistributeCacheProviderCollection : ProviderCollection
	{
		public override void Add(ProviderBase provider)
		{
			if (provider == null)
			{
				throw new ArgumentNullException("provider");
			}
			if (!(provider is DistributeCacheProvider))
			{
				throw new ArgumentException(String.Format("分布式缓存提供程序的类型 {0} 必须实现或继承 XMS.Core.Configuration.DistributeCacheProvider", provider.GetType().FullName));
			}
			base.Add(provider);
		}

		public new DistributeCacheProvider this[string name]
		{
			get
			{
				return (DistributeCacheProvider)base[name];
			}
		}
	}
}
