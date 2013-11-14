using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Entity
{
	public class DatabaseNotExistException : Exception
	{
		private string databaseName;

		public string DatabaseName
		{
			get
			{
				return this.databaseName;
			}
		}

		public DatabaseNotExistException(string databaseName)
			: base(String.Format("数据库 {0} 不存在。", databaseName))
		{
			this.databaseName = databaseName;
		}
	}
}
