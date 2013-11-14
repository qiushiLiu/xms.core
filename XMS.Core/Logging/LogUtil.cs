using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using log4net;
using log4net.Core;

namespace XMS.Core.Logging
{
	public class LogUtil
	{
		public static void Debug(string logFile, string message, string category, Exception exception)
		{
			LogToFile(logFile, message, Level.Debug, category, exception);
		}

		public static void Info(string logFile, string message, string category, Exception exception)
		{
			LogToFile(logFile, message, Level.Info, category, exception);
		}

		public static void Warn(string logFile, string message, string category, Exception exception)
		{
			LogToFile(logFile, message, Level.Warn, category, exception);
		}

		public static void Error(string logFile, string message, string category, Exception exception)
		{
			LogToFile(logFile, message, Level.Error, category, exception);
		}

		public static void Fatal(string logFile, string message, string category, Exception exception)
		{
			LogToFile(logFile, message, Level.Fatal, category, exception);
		}

		internal static void LogToUnhandledExceptions(string message)
		{
			if (!String.IsNullOrEmpty(message))
			{
				try
				{
					using (System.IO.FileStream fs = new System.IO.FileStream(AppDomain.CurrentDomain.MapPhysicalPath("logs\\unhandledExceptions.log"), System.IO.FileMode.Append, System.IO.FileAccess.Write))
					{
						using (System.IO.StreamWriter w = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8))
						{
							w.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("MM-dd HH:mm:ss.fff"), message));
						}
					}
				}
				catch{}
			}
		}

		// 把日志写入到 ..\logs\error.log
		internal static void LogToErrorLog(string message, Level level, string category, Exception exception)
		{
			LogToFile("error.log", message, level, category, exception);
		}

		internal static void LogToFile(string logFile, string message, Level level, string category, Exception exception)
		{
			string formatedMessage = null;

			if (exception == null)
			{
				formatedMessage = String.Format("{0} {1,-5} {2,-8} - {3}", new object[]{
										DateTime.Now.ToString("MM-dd HH:mm:ss.fff"),
										level.ToString(),
										category,
										message
									});
			}
			else if (!String.IsNullOrEmpty(message))
			{
				formatedMessage = String.Format("{0} {1,-5} {2,-8} - {3}\r\n{4}", new object[]{
										DateTime.Now.ToString("MM-dd HH:mm:ss.fff"),
										level.ToString(),
										category,
										message,
										exception.ToString()
									});
			}
			else
			{
				formatedMessage = String.Format("{0} {1,-5} {2,-8} - {3}", new object[]{
										DateTime.Now.ToString("MM-dd HH:mm:ss.fff"),
										level.ToString(),
										category,
										exception.ToString()
									});
			}

			try
			{
				int retryCount = 0;
				while (true)
				{
					retryCount++;
					try
					{
						string fileName = AppDomain.CurrentDomain.MapPhysicalPath("logs\\" + logFile);

						if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(fileName)))
						{
							System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fileName));
						}

						using (System.IO.FileStream fs = new System.IO.FileStream(
								GetCurrentLogFile(fileName),
								System.IO.FileMode.Append,
								System.IO.FileAccess.Write, FileShare.Read)
							)
						{
							using (System.IO.StreamWriter w = new System.IO.StreamWriter(fs, Encoding.Default))
							{
								w.WriteLine(formatedMessage);
							}
						}

						break;
					}
					catch
					{
						if (retryCount >= 5)
						{
							throw;
						}
						System.Threading.Thread.Sleep(200);
					}
				}
			}
			catch (Exception err)
			{
				LogToUnhandledExceptions(
					String.Format("{0}\r\n原始要写入的消息为:\r\n{1}", err.GetFriendlyToString(), message)
					);
			}
		}

		#region 根据指定的文件获取具有索引的文件路径 具体实现参考 CustomFileAppender
		// .../logs/error.log --> .../logs/error.{n}.log
		private static string GetCurrentLogFile(string baseFile)
		{
			int currentIndex = InitializeRollBackups(baseFile, GetExistingFiles(baseFile));

			string currentFile = Path.Combine(Path.GetDirectoryName(baseFile), Path.GetFileNameWithoutExtension(baseFile) + "." + currentIndex + Path.GetExtension(baseFile));

			if (File.Exists(currentFile))
			{
				System.IO.FileInfo fi = new FileInfo(currentFile);

				if (fi.Length > 1024 * 1024)
				{
					currentFile = Path.Combine(Path.GetDirectoryName(baseFile), Path.GetFileNameWithoutExtension(baseFile) + "." + (currentIndex + 1) + Path.GetExtension(baseFile));
				}
			}

			return currentFile;
		}

		private static List<string> GetExistingFiles(string baseFilePath)
		{
			List<string> alFiles = new List<string>();

			string directory = null;

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
			return alFiles;
		}

		private static int InitializeRollBackups(string baseFile, List<string> arrayFiles)
		{
			int currentIndex = 1;
			
			if (arrayFiles != null)
			{
				string baseFileLower = baseFile.ToLower(System.Globalization.CultureInfo.InvariantCulture);

				foreach (string curFileName in arrayFiles)
				{
					currentIndex = InitializeFromOneFile(baseFileLower, curFileName.ToLower(System.Globalization.CultureInfo.InvariantCulture), currentIndex);
				}
			}

			return currentIndex;
		}

		private static int InitializeFromOneFile(string baseFile, string curFileName, int currentIndex)
		{
			if (curFileName.StartsWith(Path.GetFileNameWithoutExtension(baseFile)) == false)
			{
				// This is not a log file, so ignore
				return currentIndex;
			}
			if (curFileName.Equals(baseFile))
			{
				// Base log file is not an incremented logfile (.1 or .2, etc)
				return currentIndex;
			}

			try
			{
				int backupIndex = GetBackUpIndex(curFileName);

				if (backupIndex > currentIndex)
				{
					return backupIndex;
				}
			}
			catch{}

			return currentIndex;
		}

		private static int GetBackUpIndex(string curFileName)
		{
			string fileName = Path.GetFileNameWithoutExtension(curFileName);

			int index = fileName.LastIndexOf(".");

			return fileName.Substring(index + 1).ConvertToInt32(-1);
		}
		#endregion
	}
}
