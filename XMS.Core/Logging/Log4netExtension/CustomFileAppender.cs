using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using log4net.Util;
using log4net.Core;
using log4net.Appender;

namespace XMS.Core.Logging.Log4net
{
	/// <summary>
	/// 自定义日志文件输出器。
	/// </summary>
	public class CustomFileAppender : FileAppender, IAppenderEnable
    {
		private bool enable = true;
		/// <summary>
		/// 获取一个值，该值指示是否启用当前输出器。
		/// </summary>
		public bool Enable
		{
			get
			{
				return this.enable;
			}
			set
			{
				this.enable = value;
			}
		}

		private bool directoryByDate = false;
		/// <summary>
		/// 获取一个值，该值指示是否按日期对日志进行分目录。
		/// </summary>
		public bool DirectoryByDate
		{
			get
			{
				return this.directoryByDate;
			}
			set
			{
				this.directoryByDate = value;
			}
		}

		private int keepDays = 7;

		/// <summary>
		/// 获取一个值，该值指示日志保留天数，该选项仅在 DirectoryByDate 为 true 时有效。
		/// </summary>
		public int KeepDays
		{
			get
			{
				return this.keepDays;
			}
			set
			{
				this.keepDays = value;
			}
		}

		private string baseFileName;

		/// <summary>
		/// Holds date of last roll over
		/// </summary>
		private DateTime lastOpenFileOfDate;

		/// <summary>
		/// The default maximum file size is 10MB
		/// </summary>
		private long m_maxFileSize = 10 * 1024 * 1024;

		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size in bytes that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property is equivalent to <see cref="MaximumFileSize"/> except
		/// that it is required for differentiating the setter taking a
		/// <see cref="long"/> argument from the setter taking a <see cref="string"/> 
		/// argument.
		/// </para>
		/// <para>
		/// The default maximum file size is 10MB (10*1024*1024).
		/// </para>
		/// </remarks>
		public long MaxFileSize
		{
			get { return m_maxFileSize; }
			set { m_maxFileSize = value; }
		}
  
		/// <summary>
		/// Gets or sets the maximum size that the output file is allowed to reach
		/// before being rolled over to backup files.
		/// </summary>
		/// <value>
		/// The maximum size that the output file is allowed to reach before being 
		/// rolled over to backup files.
		/// </value>
		/// <remarks>
		/// <para>
		/// This property allows you to specify the maximum size with the
		/// suffixes "KB", "MB" or "GB" so that the size is interpreted being 
		/// expressed respectively in kilobytes, megabytes or gigabytes. 
		/// </para>
		/// <para>
		/// For example, the value "10KB" will be interpreted as 10240 bytes.
		/// </para>
		/// <para>
		/// The default maximum file size is 10MB.
		/// </para>
		/// <para>
		/// If you have the option to set the maximum file size programmatically
		/// consider using the <see cref="MaxFileSize"/> property instead as this
		/// allows you to set the size in bytes as a <see cref="Int64"/>.
		/// </para>
		/// </remarks>
		public string MaximumFileSize
		{
			get { return m_maxFileSize.ToString(NumberFormatInfo.InvariantInfo); }
			set { m_maxFileSize = OptionConverter.ToFileSize(value, m_maxFileSize + 1); }
		}

		public CustomFileAppender() 
		{
		}

		#region Override implementation of FileAppender 
  
		/// <summary>
		/// Sets the quiet writer being used.
		/// </summary>
		/// <remarks>
		/// This method can be overridden by sub classes.
		/// </remarks>
		/// <param name="writer">the writer to set</param>
		override protected void SetQWForFiles(TextWriter writer) 
		{
			QuietWriter = new CountingQuietTextWriter(writer, ErrorHandler);
		}

		/// <summary>
		/// Write out a logging event.
		/// </summary>
		/// <param name="loggingEvent">the event to write to file.</param>
		/// <remarks>
		/// <para>
		/// Handles append time behavior for RollingFileAppender.  This checks
		/// if a roll over either by date (checked first) or time (checked second)
		/// is need and then appends to the file last.
		/// </para>
		/// </remarks>
		override protected void Append(LoggingEvent loggingEvent) 
		{
			if (this.Enable)
			{
				AdjustFileBeforeAppend();

				base.Append(loggingEvent);
			}
		}
  
 		/// <summary>
		/// Write out an array of logging events.
		/// </summary>
		/// <param name="loggingEvents">the events to write to file.</param>
		/// <remarks>
		/// <para>
		/// Handles append time behavior for RollingFileAppender.  This checks
		/// if a roll over either by date (checked first) or time (checked second)
		/// is need and then appends to the file last.
		/// </para>
		/// </remarks>
		override protected void Append(LoggingEvent[] loggingEvents) 
		{
			if (this.Enable)
			{
				foreach (LoggingEvent loggingEvent in loggingEvents)
				{
					this.Append(loggingEvent);
				}
			}
		}

		private void AdjustFileBeforeAppend()
		{
			// 未打开过时打开，之所以这样做，是为了避免在 ActivateOptions 时就创建日志文件，只在第一次输出时才创建日志文件
			if (!this.baseOptionsActivated)
			{
				this.SafeOpenFile(this.baseFileName, this.AppendToFile);

				this.baseOptionsActivated = true;
			}

			// 当天的永远在日志文件根目录
			// 如果启用按日期分类日志，则以前的按日期归类
			//	否则以前的也永远存在日志文件根目录
			if( this.DirectoryByDate && lastOpenFileOfDate < DateTime.Now.Date)
			{
				this.CloseFile();

				this.ClearExpiredLogs();

				List<string> files = this.GetExistingFiles(this.baseFileName);

				this.RollOverTime(files, lastOpenFileOfDate);

				this.SafeOpenFile(this.baseFileName, false);
			}
			else if (((CountingQuietTextWriter)QuietWriter).Count >= m_maxFileSize)
			{
				this.CloseFile();

				m_curBackupIndex++;

				SafeOpenFile(this.baseFileName, false);
			}
		}

		protected override void SafeOpenFile(string fileName, bool append)
		{
			// 避免在 ActivateOptions 时就创建日志文件
			if (!this.isInActivateOptions)
			{
				base.SafeOpenFile(fileName, append);
			}
		}

		protected override bool PreAppendCheck()
		{
			if (this.Enable)
			{
				return base.PreAppendCheck();
			}
			return false;
		}

		protected override void WriteFooter()
		{
			if (this.QuietWriter != null && !this.QuietWriter.Closed)
			{
				base.WriteFooter();
			}
		}

		protected override void CloseWriter()
		{
			if (this.QuietWriter != null && !this.QuietWriter.Closed)
			{
				base.CloseWriter();
			}
		}

		protected override void OpenFile(string fileName, bool append)
		{
			lock(this)
			{
				fileName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "." + this.m_curBackupIndex + Path.GetExtension(fileName));

				long currentCount = 0;
				if (append) 
				{
					using(SecurityContext.Impersonate(this))
					{
						if (System.IO.File.Exists(fileName))
						{
							currentCount = (new FileInfo(fileName)).Length;
						}
					}
				}

				base.OpenFile(fileName, append);

				((CountingQuietTextWriter)QuietWriter).Count = currentCount;

				this.lastOpenFileOfDate = DateTime.Now.Date;
			}
		}
		#endregion

		private int m_curBackupIndex = 1;

		// 以下两个变量避免在 ActivateOptions 时就创建日志文件
		private bool isInActivateOptions = false;
		private bool baseOptionsActivated = false;

		/// <summary>
		/// ActivateOptions
		/// </summary>
		public override void ActivateOptions() 
		{
			if (this.Enable)
			{
				if (SecurityContext == null)
				{
					SecurityContext = SecurityContextProvider.DefaultProvider.CreateSecurityContext(this);
				}

				using (SecurityContext.Impersonate(this))
				{
					base.File = ConvertToFullPath(base.File.Trim());
				}

				this.baseFileName = base.File;

				List<string> files = DetermineCurSizeRollBackups();

				if (this.DirectoryByDate)
				{
					this.ClearExpiredLogs();

					string curFileName = Path.Combine(Path.GetDirectoryName(this.baseFileName), Path.GetFileNameWithoutExtension(this.baseFileName) + "." + this.m_curBackupIndex + Path.GetExtension(this.baseFileName));

					if (System.IO.File.Exists(curFileName))
					{
						DateTime creationTime = System.IO.File.GetCreationTime(curFileName);
						if (creationTime.Date < DateTime.Now.Date)
						{
							this.RollOverTime(files, creationTime);
						}
					}
				}

				// 调用基类的 ActivateOptions 进行初始化
				// 但使用 isInActivateOptions 变量避免在 ActivateOptions 时就创建日志文件
				// 具体请参考 base.ActivateOptions 的实现。
				try
				{
					this.isInActivateOptions = true;
					base.ActivateOptions();
				}
				finally
				{
					this.isInActivateOptions = false;
				}
			}
		}

		private string[] directoryNameByDateFormats = new string[] { "yyyy-MM-dd", "yyyy-M-dd", "yyyy-MM-d", "yyyy-M-d" };

		private void RollOverTime(List<string> files, DateTime date)
		{
			// 将当日日志移动到日期内归档
			string baseDirectory = Path.GetDirectoryName(this.baseFileName) + "\\";
			string directory = baseDirectory + date.ToString(directoryNameByDateFormats[0]) + "\\";
			if (!System.IO.Directory.Exists(directory))
			{
				System.IO.Directory.CreateDirectory(directory);
			}

			for (int i = 0; i < files.Count; i++)
			{
				try
				{
					System.IO.File.Move(baseDirectory + files[i], directory + Path.GetFileName(files[i]));
				}
				catch
				{
					System.IO.File.Copy(baseDirectory  + files[i], directory + Path.GetFileName(files[i]), true);

					System.IO.File.Delete(baseDirectory + files[i]);
				}
			}

			this.m_curBackupIndex = 1;
		}

		// 清理 KeepDays 属性指定天数之前的日志归档文件
		private void ClearExpiredLogs()
		{
			// 将当日日志移动到日期内归档
			string baseDirectory = Path.GetDirectoryName(this.baseFileName) + "\\";

			if (System.IO.Directory.Exists(baseDirectory))
			{
				string[] directories = Directory.GetDirectories(baseDirectory, "*", SearchOption.TopDirectoryOnly);
				for (int i = 0; i < directories.Length; i++)
				{
					try
					{
						DateTime directoryTime = DateTime.ParseExact(Path.GetFileName(directories[i]), directoryNameByDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None);

						if (directoryTime.AddDays(this.KeepDays) < DateTime.Now.Date)
						{
							Directory.Delete(directories[i], true);
						}
					}
					catch
					{
					}
				}
			}
		}

		/// <summary>
		///	Determines curSizeRollBackups (only within the current roll point)
		/// </summary>
		private List<string> DetermineCurSizeRollBackups()
		{
			string fullPath = null;
			string fileName = null;

			using (SecurityContext.Impersonate(this))
			{
				fullPath = System.IO.Path.GetFullPath(this.baseFileName);
				fileName = System.IO.Path.GetFileName(fullPath);
			}

			List<string> arrayFiles = GetExistingFiles(fullPath);

			InitializeRollBackups(fileName, arrayFiles);

			return arrayFiles;
		}

		private List<string> GetExistingFiles(string baseFilePath)
		{
			List<string> alFiles = new List<string>();

			string directory = null;

			using (SecurityContext.Impersonate(this))
			{
				string fullPath = Path.GetFullPath(baseFilePath);

				directory = Path.GetDirectoryName(fullPath);

				if (Directory.Exists(directory))
				{
					string baseFileName = Path.GetFileName(fullPath);

					string[] files = Directory.GetFiles(directory, Path.GetFileNameWithoutExtension(baseFileName) + ".*" + Path.GetExtension(baseFileName));

					if (files != null)
					{
						for (int i = 0; i < files.Length; i++)
						{
							string curFileName = Path.GetFileName(files[i]);
							if (curFileName.StartsWith(Path.GetFileNameWithoutExtension(baseFileName)))
							{
								alFiles.Add(curFileName);
							}
						}
					}
				}
			}
			return alFiles;
		}

		private void InitializeRollBackups(string baseFile, List<string> arrayFiles)
		{
			if (null != arrayFiles)
			{
				string baseFileLower = baseFile.ToLower(System.Globalization.CultureInfo.InvariantCulture);

				foreach (string curFileName in arrayFiles)
				{
					InitializeFromOneFile(baseFileLower, curFileName.ToLower(System.Globalization.CultureInfo.InvariantCulture));
				}
			}
		}

		private void InitializeFromOneFile(string baseFile, string curFileName)
		{
			if (curFileName.StartsWith(Path.GetFileNameWithoutExtension(baseFile)) == false)
			{
				// This is not a log file, so ignore
				return;
			}
			if (curFileName.Equals(baseFile))
			{
				// Base log file is not an incremented logfile (.1 or .2, etc)
				return;
			}

			try
			{
				int backup = GetBackUpIndex(curFileName);

				if (backup > m_curBackupIndex)
				{
					m_curBackupIndex = backup;
				}
			}
			catch{}
		}

		private int GetBackUpIndex(string curFileName)
		{
			string fileName = Path.GetFileNameWithoutExtension(curFileName);

			int index = fileName.LastIndexOf(".");

			return fileName.Substring(index + 1).ConvertToInt32(-1);
		}
	}
}
