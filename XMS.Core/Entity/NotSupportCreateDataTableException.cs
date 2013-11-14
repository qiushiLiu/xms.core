using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Entity
{
	public class NotSupportCreateDataTableException : Exception
	{
		private string databaseName;
		private string tableName;

		public string DatabaseName
		{
			get
			{
				return this.databaseName;
			}
		}

		public string TableName
		{
			get
			{
				return this.tableName;
			}
		}

		public NotSupportCreateDataTableException(string databaseName, string tableName)
			: base(String.Format("不支持在数据库 {0} 中创建表 {1}。", databaseName, tableName))
		{
			this.databaseName = databaseName;
			this.tableName = tableName;
		}
	}
}
