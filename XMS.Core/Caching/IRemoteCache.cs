using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using XMS.Core.Logging;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 远程缓存接口
	/// </summary>
	public interface IRemoteCache : ICache
	{
	}
}
