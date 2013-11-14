using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Entity
{
	public class DataTableNotExistException : Exception
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

		public DataTableNotExistException(string databaseName, string tableName)
			: base(String.Format("数据库 {0} 中不存在名称为 {1} 的表。", databaseName, tableName))
		{
			this.databaseName = databaseName;
			this.tableName = tableName;
		}
	}
}
