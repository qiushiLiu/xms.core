using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace XMS.Core.Caching
{
	/// <summary>
	/// 在存储于本地缓存对象中的项与文件、缓存键、文件或缓存键的数组或另一个 CacheDependency 对象之间建立依附性关系。 
	/// CacheDependency 类监视依附性关系，以便在任何这些对象更改时，该缓存项都会自动移除。 
	/// </summary>
	/// <remarks>
	/// CacheDependency类提供两种方式以判断文件是否发生变化：
	///		HasChanged 属性，通过主动获取该属性，业务逻辑可直接判断文件自上次加载后是否发生变化；
	///		事件通知机制
	///	一旦文件发生变化，FileWatcher 对象就会从监视列表中移除，并不再监测其关联的文件后续发生的任何变化，也就无法接收到任何与该文件关联的事件变化通知；
	///	可以通过以下方式继续监视文件的变化：
	///		当发现或监听到 FileWatcher 关联的文件已经发生变化后，将业务相关的 FileWatcher 设为 null， 然后在需要的时候重新通过 FileWatcher.Get 方法获取
	///	最新的与指定文件关联的 FileWatcher 对象，该对象的 HasChanged 属性为 false
	///	详细示例请参考 缓存服务和配置服务 中通过本类监测关联文件是否发生变化的示例和用法。
	/// </remarks>
	public class CacheDependency : IDisposable
	{
		private static Dictionary<string, CacheDependency> dependencies = new Dictionary<string, CacheDependency>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// 使用指定的文件名或目录获取一个依赖项，如果与指定的文件名或目录对应的依赖项不存在，那么新建一个与其关联的依赖项并返回它。
		/// </summary>
		/// <param name="fileOrDirectory">指定的文件名或目录。</param>
		/// <returns>CacheDependency 对象。</returns>
		/// <remarks>
		/// 如果传入的文件名或目录不合法，那么该方法返回 null。
		/// </remarks>
		public static CacheDependency Get(string fileOrDirectory)
		{
			if (!String.IsNullOrEmpty(fileOrDirectory))
			{
				// fileOrDirectory	GetFileName		GetDirectoryName
				// \zhaixd\a\b		> b				\zhaixd\a
				// D:\a\b			> b				D:\a
				// D:\a\b\			> String.Empty	D:\a\b
				// \zhaixd\a\b.txt	> b.txt			\zhaixd\a
				// D:\a\b.txt		> b.txt			D:\a
				// D:\				> String.Empty	null
				// a				> String.Empty	String.Empty
				string directoryName = Path.GetDirectoryName(fileOrDirectory);
				if (!String.IsNullOrEmpty(directoryName))
				{
					string fileName = Path.GetFileName(fileOrDirectory);

					lock (dependencies)
					{
						CacheDependency cacheDependency;
						if (dependencies.ContainsKey(fileOrDirectory))
						{
							cacheDependency = dependencies[fileOrDirectory];
							if (!cacheDependency.hasChanged)
							{
								return cacheDependency;
							}
						}

						cacheDependency = new CacheDependency(fileOrDirectory, directoryName, fileName);
						dependencies.Add(fileOrDirectory, cacheDependency);
						cacheDependency.fsw.EnableRaisingEvents = true;
						return cacheDependency;
					}
				}
			}
			return null;
		}

		private static void Remove(CacheDependency dependency)
		{
			lock (dependencies)
			{
				dependencies.Remove(dependency.fileOrDirectory);
			}
		}

		private string fileOrDirectory;

		private bool hasChanged = false;

		private FileSystemWatcher fsw;

		/// <summary>
		/// 表示文件变化的事件
		/// </summary>
		internal event FileSystemEventHandler Changed;

		/// <summary>
		/// 使用指定的文件名或目录初始化 CacheDependency 类的新实例。
		/// </summary>
		/// <param name="fileOrDirectory"></param>
		/// <param name="directoryName">目录名</param>
		/// <param name="fileName">文件名</param>
		private CacheDependency(string fileOrDirectory, string directoryName, string fileName)
		{
			if (String.IsNullOrEmpty(fileOrDirectory))
			{
				throw new ArgumentException("fileOrDirectory");
			}

			if (String.IsNullOrEmpty(directoryName))
			{
				throw new ArgumentException("directoryName");
			}

			this.fileOrDirectory = fileOrDirectory;

			this.fsw = String.IsNullOrEmpty(fileName) ? new FileSystemWatcher(directoryName) : new FileSystemWatcher(directoryName, fileName);

			// 初始化 fsw，但不启用它，直到注册了事件才启用它，参见 FileWatcher.Changed 事件的实现
			this.fsw.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;

			this.fsw.IncludeSubdirectories = false;

			this.fsw.Changed += new System.IO.FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Created += new FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Deleted += new FileSystemEventHandler(this.fsw_Changed);
			this.fsw.Renamed += new RenamedEventHandler(this.fsw_Changed);

			this.fsw.Error += new ErrorEventHandler(fsw_Error);
		}

		private bool hasWin32Error = false;
		private DateTime? win32ErrorTime = null;

		void fsw_Error(object sender, ErrorEventArgs e)
		{
			Exception err = e.GetException();
			// 当检测网络路径时，如果网络断开，则会爆 Code 为 64 的 Win32 异常
			if (err is System.ComponentModel.Win32Exception)
			{
				this.hasWin32Error = true;

				this.win32ErrorTime = DateTime.Now;

				XMS.Core.Container.LogService.Warn(
					String.Format("在监测“{0}”时 发生 Win32 错误，错误码为：{1}，详细错误信息为：{2}"
					,this.fileOrDirectory, ((System.ComponentModel.Win32Exception)err).NativeErrorCode, ((System.ComponentModel.Win32Exception)err).Message)
					, Logging.LogCategory.Cache);
			}
			else // 经查看 FileSystemWatcher 的内部实现，基本上不会抛出其它类型的异常
			{
				XMS.Core.Container.LogService.Warn(
					String.Format("在监测“{0}”时 发生错误，详细错误信息为：{1}"
					, this.fileOrDirectory, err.Message)
					, Logging.LogCategory.Cache);
			}
		}

		#region 文件变化事件和文件变化监视
		/// <summary>
		/// 获取一个值，该值指示当前依赖项是否已经发生变化。
		/// </summary>
		public bool HasChanged
		{
			get
			{
				if (this.hasWin32Error)
				{
					lock (this.fsw)
					{
						if (this.hasWin32Error)
						{
							try
							{
								this.fsw.EnableRaisingEvents = false;

								this.fsw.EnableRaisingEvents = true;

								this.hasWin32Error = false;

								XMS.Core.Container.LogService.Warn(
									String.Format("已成功恢复对“{0}”的监测。", this.fileOrDirectory)
									, Logging.LogCategory.Cache);
							}
							catch (Exception err)
							{
								if (XMS.Core.Container.LogService.IsDebugEnabled)
								{
									XMS.Core.Container.LogService.Debug(
										String.Format("未能恢复对“{0}”的监测，详细错误信息为：{1}",
										this.fileOrDirectory, err.Message),
										Logging.LogCategory.Cache);
								}
							}
						}
					}
				}

				return this.hasChanged;
			}
		}

		// 文件变化时仅将当前依赖项设置为已变化
		private void fsw_Changed(object sender, FileSystemEventArgs e)
		{
			if (this.fsw != null && !this.hasChanged)
			{
				lock (this.fsw)
				{
					if (this.fsw != null && !this.hasChanged)
					{
						try
						{
							this.hasChanged = true;

							Remove(this);

							// 引发目标事件
							if (this.Changed != null)
							{
								this.Changed(this, e);
							}
						}
						catch (Exception err)
						{
							Container.LogService.Warn(String.Format("在处理缓存依赖文件\"{0}\"变化事件的过程中发生错误", this.fileOrDirectory), Logging.LogCategory.Cache, err);
						}
						finally
						{
							this.Dispose();
						}
					}
				}
			}
		}
		#endregion

		#region IDisposable interface
		private bool disposed = false;

		/// <summary>
		/// 释放资源。
		/// </summary>
		public void Dispose()
		{
			Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.fsw != null)
					{
						this.fsw.Dispose();
						this.fsw = null;
					}

					this.Changed = null;
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~CacheDependency()
		{
			Dispose(false);
		}
		#endregion
	}
}