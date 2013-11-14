/* ======================================================================
 * Copyright (C) 2004-2005 zhaixd@hotmail.com. All rights reserved.
 * FileName		 : IEntityContext.cs
 * Author		 : 翟雪东
 * Date			 : 2004-12-24
 * Version		 : 1.0
 * 
 * This software is the confidential and proprietary information of 
 * zhaixd@hotmail.com ("Confidential Information").  
 * You shall not disclose such Confidential Information and shall
 * use it only in accordance with the terms of the license agreement 
 * you entered into with zhaixd@hotmail.com.
 * ======================================================================
 * History (历史修改记录)
 *	<author>	   <time>		<version>	<description>
 *	翟雪东		2004-12-24		   1.0		build this moudle  
 */

using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;

using XMS.Core.Entity;

namespace XMS.Core.Data
{
	/// <summary>
	/// <b>DataTableAdapter</b> 提供 DataTable 和数据库之间的通信。
	/// </summary>
	/// <remarks>
	/// 只能使用 <see cref="IEntityContext"/> 的 <see cref="IDatabase.CreateDataTableAdapter()"/> 方法创建 <see cref="DataTableAdapter"/> 的实例，
	/// <b>DataTableAdapter</b> 通过 <b>IEntityContext</b> 提供的连接及事务（如果开始了事务的话）与数据库进行通信、执行查询或存储过程，
	/// 并返回用返回数据填充的新数据表或是用返回数据填充现有 <b>DataTableAdapter</b>。
	/// <b>DataTableAdapter</b> 还用于将更新数据从应用程序发送回数据库。
	/// </remarks>
	/// <example>
	/// 初始化：
	/// 	DataTableAdapter adapter = entityContext.CreateDataTableAdapter();
	/// 填充表:
	/// 	DataTabale table = new DataTable(entityContext.GetPartitionTableName("Order"));
	///   不需要参数时：
	/// 	adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>10", parameters).Fill(table);
	///   简化命令写法时：
	///		Dictionary&lt;string, object&gt; parameters = new Dictionary&lt;string, object&gt;(1);
	///		parameters["OrderId"] = 10;  或者 parameters["@OrderId"] = 10;
	///		adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>@OrderId", parameters).Fill(table);
	///	  完整命令写法时：
	///		adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order") + " where OrderId>@OrderId", 
	///			new System.Data.Common.DbParameter[]{
	///				adapter.CreateParameter("OrderId", DbType.Int32, 10)
	///			}).Fill(table);
	/// 使用自动命令更新对表的更改:
	///		adapter.BuildCommands().Update(table);
	/// 使用自定义命令更新表:
	///	  1.设置Update命令:
	///		简化命令写法：
	///		adapter.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			"Title", "CustomerName", "OrderId", "A");
	///		完整命令写法：
	///		adapter.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			new System.Data.Common.DbParameter[]{
	///					adapter.CreateParameter("Title", DbType.String, 100),
	///					adapter.CreateParameter("@CustomerName", DbType.String, 200),
	///					adapter.CreateParameter("OrderId", DbType.Int32)
	///			});
	///	  2.设置Delete命令： 简化和完整两种写法分别参考Update命令
	///		adapter.SetDeleteCommand(...);
	///	  3.设置Insert命令： 简化和完整两种写法分别参考Update命令
	///		adapter.SetInsertCommand(...);
	///	  4.执行更新	
	///		this.dataTableAdapter.Update(table); 或者 this.dataTableAdapter.Update(table.Rows); 或者 this.dataTableAdapter.Update(dataset, tableName);
	///	创建自定义命令并执行
	///		using(DbCommand command = adapter.CreateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
	///			new System.Data.Common.DbParameter[]{
	///					adapter.CreateParameter("Title", DbType.String, 100),
	///					adapter.CreateParameter("@CustomerName", DbType.String, 200),
	///					adapter.CreateParameter("OrderId", DbType.Int32)
	///			})
	///		{
	///			foreach(var item in list)
	///			{
	///				command.Parameters["@Title"] = item.Title;
	///				command.Parameters["@CustomerName"] = item.CustomerName;
	///				command.Parameters["@OrderId"] = item.OrderId;
	///				
	///				command.ExecuteNoneQuery();
	///			}
	///		}
	/// </example>
	public class DataTableAdapter : IDisposable
	{
		/// <summary>
		/// Specifies the action that command is supposed to perform, i.e. Select, Insert, Update, Delete.
		/// It is used in Execute methods of the <see cref="IEntityContext"/> class to identify command instance 
		/// to be used.
		/// </summary>
		private enum CommandAction
		{
		    Select,
		    Insert,
		    Update,
		    Delete,
			Other
		}

		private DbCommand _selectCommand = null;
		private DbCommand _insertCommand = null;
		private DbCommand _updateCommand = null;
		private DbCommand _deleteCommand = null;
		private DbParameter[] _selectCommandParameters;
		private DbParameter[] _insertCommandParameters;
		private DbParameter[] _updateCommandParameters;
		private DbParameter[] _deleteCommandParameters;

		#region Public Properties
		/// <summary>
		/// Gets the select <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>SelectCommand</b> can be used to access select command parameters.
		/// </remarks>
		protected DbCommand SelectCommand
		{
			get
			{
				return this._selectCommand;
			}
		}

		/// <summary>
		/// Gets the insert <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>InsertCommand</b> can be used to access insert command parameters.
		/// </remarks>
		protected DbCommand InsertCommand
		{
			get
			{
				return this._insertCommand;
			}
		}

		/// <summary>
		/// Gets the update <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>UpdateCommand</b> can be used to access update command parameters.
		/// </remarks>
		protected DbCommand UpdateCommand
		{
			get
			{
				return this._updateCommand;
			}
		}

		/// <summary>
		/// Gets the delete <see cref="DbCommand"/> used by this instance of the <see cref="IEntityContext"/>.
		/// </summary>
		/// <value>
		/// A <see cref="DbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>DeleteCommand</b> can be used to access delete command parameters.
		/// </remarks>
		protected DbCommand DeleteCommand
		{
			get
			{
				return this._deleteCommand;
			}
		}
		#endregion

		private DbProviderFactory dbProviderFactory;
		private DbConnection connection;
		private DbTransaction transaction;

		internal DataTableAdapter(DbProviderFactory dbProviderFactory, DbConnection connection, DbTransaction transaction)
		{
			if (dbProviderFactory == null)
			{
				throw new ArgumentNullException("dbProviderFactory");
			}
			if (connection == null)
			{
				throw new ArgumentNullException("connection");
			}
			this.dbProviderFactory = dbProviderFactory;
			this.connection = connection;
			this.transaction = transaction;

			switch (this.connection.State)
			{
				case ConnectionState.Closed:
					this.connection.Open();
					break;
			}
		}

		#region IDisposable interface
		private bool disposed = false;
		// Implement Idisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		//与 Close 方法实现的功能相同
		public void Dispose()
		{
			Dispose(true);
			// Take yourself off of the Finalization queue 
			// to prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="IEntityContext"/> and 
		/// optionally releases the managed resources.
		/// </summary>
		/// <param name="disposing"><b>true</b> to release both managed and unmanaged resources; <b>false</b> to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this._selectCommand != null)
					{
						this._selectCommand.Dispose();
						this._selectCommand = null;
					}
					if (this._insertCommand != null)
					{
						this._insertCommand.Dispose();
						this._insertCommand = null;
					}
					if (this._updateCommand != null)
					{
						this._updateCommand.Dispose();
						this._updateCommand = null;
					}
					if (this._deleteCommand != null)
					{
						this._deleteCommand.Dispose();
						this._deleteCommand = null;
					}
					if (this._adapter != null)
					{
						this._adapter.Dispose();
						this._adapter = null;
					}
					if (this._builder != null)
					{
						this._builder.Dispose();
						this._builder = null;
					}
				}
			}
			disposed = true;
		}
		~DataTableAdapter()
		{
			Dispose(false);
		}
		#endregion

		#region 创建和设置用于执行数据库查询操作的增删改查命令
		private DbCommand CreateCommand()
		{
			DbCommand cmd = this.connection.CreateCommand();
			if (this.transaction != null)
			{
				cmd.Transaction = this.transaction;
			}
			return cmd;
		}
		private DbCommand GetCommand(CommandAction commandAction)
		{
			switch (commandAction)
			{
				case CommandAction.Insert:
					if (this._insertCommand == null)
					{
						this._insertCommand = this.CreateCommand();
					}
					return this._insertCommand;
				case CommandAction.Update:
					if (this._updateCommand == null)
					{
						this._updateCommand = this.CreateCommand();
					}
					return this._updateCommand;
				case CommandAction.Delete:
					if (this._deleteCommand == null)
					{
						this._deleteCommand = this.CreateCommand();
					}
					return this._deleteCommand;
				case CommandAction.Select:
					if (this._selectCommand == null)
					{
						this._selectCommand = this.CreateCommand();
					}
					return this._selectCommand;
				default:
					return this.CreateCommand();
			}
		}

		private DbCommand SetCommandInternal(CommandAction cmdAction, CommandType cmdType, string cmdText, DbParameter[] cmdParameters, bool autoPrepareCommand)
		{
			DbCommand command = this.GetCommand(cmdAction);

			command.Parameters.Clear();
			command.CommandType = cmdType;

			command.CommandText = this.PretreatmentCommandText(cmdText);

			switch (cmdAction)
			{
				case CommandAction.Select:
					_selectCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.Dispose();
						this._adapter = null;
					}
					if (this._builder != null)
					{
						this._builder.Dispose();
						this._builder = null;
					}
					break;
				case CommandAction.Insert:
					_insertCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.InsertCommand = command;
					}
					break;
				case CommandAction.Update:
					_updateCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.UpdateCommand = command;
					}
					break;
				case CommandAction.Delete:
					_deleteCommandParameters = cmdParameters;
					if (this._adapter != null)
					{
						this._adapter.DeleteCommand = command;
					}
					break;
				default:
					break;
			}
			if (cmdParameters != null)
			{
				for (int i = 0; i < cmdParameters.Length; i++)
				{
					command.Parameters.Add(cmdParameters[i]);
				}
			}
			if (autoPrepareCommand)
			{
				command.Prepare();
			}
			return command;
		}

		private DataTableAdapter SetCommand(CommandAction cmdAction, CommandType cmdType, string cmdText, DbParameter[] cmdParameters, bool autoPrepareCommand)
		{
			this.SetCommandInternal(cmdAction, cmdType, cmdText, cmdParameters, autoPrepareCommand);

			return this;
		}

		public DbCommand CreateCommand(string commandText)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, null), false);
		}

		public DbCommand CreateCommand(string commandText, Dictionary<string, object> parameters)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		public DbCommand CreateCommand(string commandText, DbParameter[] parameters)
		{
			return this.SetCommandInternal(CommandAction.Other, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}


		#region 简化命令语句写法，自动推断参数类型
		/// <summary>
		/// 设置不需要任何参数的非参数化查询命令。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetSelectCommand(string commandText)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, null), false);
		}

		/// <summary>
		/// 设置查询命令。查询命令必须为其中所使用到的每个参数指定参数值。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetSelectCommand(string commandText, Dictionary<string, object> parameters)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertDictionaryToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// 设置插入命令。插入命令不必为命令所需要的每个参数指定参数值，而是在调用 Update 方法时根据 DataTable 或者 DataRow 自动推断参数值。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetInsertCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Insert, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// 设置更新命令。更新命令不必为命令所需要的每个参数指定参数值，而是在调用 Update 方法时根据 DataTable 或者 DataRow 自动推断参数值。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetUpdateCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Update, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}

		/// <summary>
		/// 设置删除命令。删除命令不必为命令所需要的每个参数指定参数值，而是在调用 Update 方法时根据 DataTable 或者 DataRow 自动推断参数值。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetDeleteCommand(string commandText, params string[] parameters)
		{
			return SetCommand(CommandAction.Delete, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, DbEntityContext.ConvertStringArrayToParameterArray(this.dbProviderFactory, true, parameters), false);
		}
		#endregion

		#region 完整命令语句写法，显示设置查询命令所需要的参数包括参数类型、长度、映射列等信息，命令生成后，自动为其调用 Prepare 方法以提高后续重复调用时的性能。
		/// <summary>
		/// 设置查询命令，显示设置查询命令所需要的参数包括参数类型、长度、映射列等信息，命令生成后，自动为其调用 Prepare 方法以提高后续重复调用时的性能。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetSelectCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Select, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// 设置插入命令，显示设置查询命令所需要的参数包括参数类型、长度、映射列等信息，命令生成后，自动为其调用 Prepare 方法以提高后续重复调用时的性能。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetInsertCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Insert, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// 设置更新命令，显示设置查询命令所需要的参数包括参数类型、长度、映射列等信息，命令生成后，自动为其调用 Prepare 方法以提高后续重复调用时的性能。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetUpdateCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Update, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}

		/// <summary>
		/// 设置删除命令，显示设置查询命令所需要的参数包括参数类型、长度、映射列等信息，命令生成后，自动为其调用 Prepare 方法以提高后续重复调用时的性能。
		/// </summary>
		/// <param name="commandText">要执行的命令文本。</param>
		/// <param name="parameters">执行命令所需要的参数数组。</param>
		/// <returns>当前 DataTableAdapter 实例。</returns>
		public DataTableAdapter SetDeleteCommand(string commandText, DbParameter[] parameters)
		{
			return SetCommand(CommandAction.Delete, DataTableAdapter.IsStoreProcedure(commandText) ? CommandType.StoredProcedure : CommandType.Text, commandText, parameters, true);
		}
		#endregion

		#region 完整命令语句写法时，调用下列语句创建命令参数
		/// <summary>
		/// 返回强类型的 <see cref="DbParameter"/> 实例。 
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="value">参数值</param>
		/// <returns><see cref="DbParameter"/> 的新强类型实例。</returns>
		public DbParameter CreateParameter(string parameterName, object value =null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.Value = value == null ? DBNull.Value : value;

			return p;
		}

		/// <summary>
		/// 返回强类型的 <see cref="DbParameter"/> 实例。 
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="dbType">参数类型</param>
		/// <returns><see cref="DbParameter"/> 的新强类型实例。</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}

		/// <summary>
		/// 返回实现 <see cref="DbParameter"/> 类的提供程序的类的一个新实例。
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="dbType">参数类型</param>
		/// <param name="isNullable">如果允许接受空值，则为 <b>true</b>；否则为 <b>false</b>。</param>
		/// <returns><see cref="DbParameter"/> 的新实例。</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, bool isNullable, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.IsNullable = isNullable;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		/// <summary>
		/// 返回强类型的 <see cref="DbParameter"/> 实例。 
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="dbType">参数类型</param>
		/// <param name="size">参数大小</param>
		/// <returns><see cref="DbParameter"/> 的新强类型实例。</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, int size, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = parameterName.Substring(1);
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = parameterName;
			}

			p.DbType = dbType;
			p.Size = size;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		/// <summary>
		/// 返回强类型的 <see cref="DbParameter"/> 实例。 
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="dbType">参数类型</param>
		/// <param name="size">参数大小</param>
		/// <param name="sourceColumn">源列的名称</param>
		/// <returns><see cref="DbParameter"/> 的新强类型实例。</returns>
		public DbParameter CreateParameter(string parameterName, DbType dbType, int size, string sourceColumn, object value = null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName.Substring(1) : sourceColumn;
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName : sourceColumn;
			}

			p.DbType = dbType;

			p.Size = size;

			return p;
		}
		/// <summary>
		/// 返回强类型的 <see cref="DbParameter"/> 实例。 
		/// </summary>
		/// <param name="parameterName">参数名称</param>
		/// <param name="dbType">参数类型</param>
		/// <param name="size">参数大小</param>
		/// <param name="parameterDirection">参数的方向</param>
		/// <param name="isNullable">如果源列可为空，则为 <b>true</b>；否则为 <b>false</b>。</param>
		/// <param name="precision">值的精度</param>
		/// <param name="scale">值的小数位数</param>
		/// <param name="sourceColumn">源列的名称</param>
		/// <param name="sourceVersion">原列的版本</param>
		/// <param name="value">参数的值</param>
		/// <returns><see cref="DbParameter"/> 的新强类型实例。</returns>
		public DbParameter CreateParameter(
			string parameterName,
			DbType dbType,
			int size,
			ParameterDirection parameterDirection,
			bool isNullable,
			byte precision,
			byte scale,
			string sourceColumn,
			DataRowVersion sourceVersion,
			object value =null)
		{
			if (String.IsNullOrWhiteSpace(parameterName))
			{
				throw new ArgumentNullOrWhiteSpaceException("parameterName");
			}

			DbParameter p = this.dbProviderFactory.CreateParameter();

			if (parameterName[0] == '@')
			{
				p.ParameterName = parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName.Substring(1) : sourceColumn;
			}
			else
			{
				p.ParameterName = "@" + parameterName;
				p.SourceColumn = String.IsNullOrWhiteSpace("sourceColumn") ? parameterName : sourceColumn;
			}

			p.DbType = dbType;
			p.Size = size;
			p.Direction = parameterDirection;
			p.IsNullable = isNullable;
			((IDbDataParameter)p).Precision = precision;
			((IDbDataParameter)p).Scale = scale;
			p.SourceVersion = sourceVersion;
			p.Value = value == null ? DBNull.Value : value;

			return p;
		}
		#endregion
		#endregion

		private DbDataAdapter _adapter = null;
		private DbCommandBuilder _builder = null;

		/// <summary>
		/// 获取用于数据库访问的数据适配器。
		/// </summary>
		protected virtual DbDataAdapter Adapter
		{
			get
			{
				if (this._adapter == null)
				{
					this._adapter = this.dbProviderFactory.CreateDataAdapter();
					this._adapter.SelectCommand = this.SelectCommand;
					this._adapter.InsertCommand = this.InsertCommand;
					this._adapter.DeleteCommand = this.DeleteCommand;
					this._adapter.UpdateCommand = this.UpdateCommand;
				}
				return this._adapter;
			}
		}

		/// <summary>
		/// 获取用于创建 SQL 执行语句的命令构建器。
		/// </summary>
		protected virtual DbCommandBuilder Builder
		{
			get
			{
				return this._builder;
			}
		}

		/// <summary>
		/// 为数据适配器创建自动命令
		/// </summary>
		/// <remarks>
		/// 自动创建的命令不覆盖自定义的插入、删除、更新命令
		/// </remarks>
		public DataTableAdapter BuildCommands()
		{
			if (this._builder == null)
			{
				if (this.InsertCommand == null || this.UpdateCommand == null || this.DeleteCommand == null)
				{
					this._builder = this.dbProviderFactory.CreateCommandBuilder();
					this._builder.DataAdapter = this.Adapter;
				}
			}
			return this;
		}

		/// <summary>
		/// 执行由 <see cref="SelectCommand"/> 指定的命令，在 <see cref="DataTable"/> 中添加或刷新行以匹配使用 <b>DataTable</b> 名称的数据源中的行。
		/// </summary>
		/// <param name="dataTable">要用记录和架构（如果必要）填充的 <see cref="DataTable"/>。</param>
		/// <returns>已填充的 <see cref="DataTable"/>。</returns>
		public int Fill(DataTable dataTable)
		{
			return this.Adapter.Fill(dataTable);
		}

		#region Update
		/// <summary>
		/// 为指定 <see cref="DataSet"/> 中指定名称的表的每个已插入、已更新或已删除的行调用相应的 <b>INSERT</b>、<b>UPDATE</b> 或 <b>DELETE</b> 语句。
		/// </summary>
		/// <param name="dataSet">用于更新数据源的 <see cref="DataSet"/>。 </param>
		/// <param name="tableName">用于表映射的源表的名称。</param>
		/// <returns><see cref="DataSet"/> 中成功更新的行数。</returns>
		/// <remarks>
		/// <b>Update(DataSet,string,bool)</b>方法调用 <c>DbDataAdapter.Update(DataSet)</c> 方法进行更新。
		/// <para>如果需要手动设置命令而未指定 INSERT、UPDATE 或 DELETE 语句，<b>Update</b> 方法会生成异常。
		/// 否则，如果设置了 <see cref="SelectCommand"/> 属性，则可以创建 <b>CommandBuilder</b>为单个表更新自动生成 SQL 语句。
		/// 然后，CommandBuilder 将生成其他任何未设置的 SQL 语句。此生成逻辑要求 <b>DataSet</b> 中存在键列信息。</para>
		/// <para>可以使用 <see cref="SetInsertCommand"/>、<see cref="SetUpdateCommand"/>、<see cref="SetDeleteCommand"/> 方法显示指定 INSERT、UPDATE 或 DELETE 语句。
		/// 使用方法示例：<br/><c>this.SetInsertCommand(string,DbParameter[])</c> ；</para>
		/// </remarks>
		public int Update(DataSet dataSet, string tableName)
		{
			if (dataSet == null)
			{
				throw new ArgumentNullException("dataSet");
			}

			if (tableName == null)
			{
				return this.Adapter.Update(dataSet);
			}
			else
			{
				return this.Adapter.Update(dataSet, tableName);
			}
		}
		/// <summary>
		/// 为指定 <see cref="DataTable"/> 中每个已插入、已更新或已删除的行调用相应的 <b>INSERT</b>、<b>UPDATE</b> 或 <b>DELETE</b> 语句。
		/// </summary>
		/// <param name="dataTable">用于更新数据源的 <see cref="DataTable"/>。</param>
		/// <returns><see cref="DataTable"/> 中成功更新的行数。</returns>
		/// <remarks>
		/// <b>Update(DataTable,bool)</b>方法调用 <c>DbDataAdapter.Update(DataTable)</c> 方法进行更新。
		/// <para>如果需要显示设置命令而未指定 INSERT、UPDATE 或 DELETE 语句，<b>Update</b> 方法会生成异常。
		/// 否则，如果设置了 <see cref="SelectCommand"/> 属性，则可以创建 <b>CommandBuilder</b>为单个表更新自动生成 SQL 语句。
		/// 然后，CommandBuilder 将生成其他任何未设置的 SQL 语句。此生成逻辑要求 <b>DataSet</b> 中存在键列信息。</para>
		/// <para>可以使用 <see cref="SetInsertCommand"/>、<see cref="SetUpdateCommand"/>、<see cref="SetDeleteCommand"/> 方法显示指定 INSERT、UPDATE 或 DELETE 语句。
		/// 使用方法示例：<br/><c>this.SetInsertCommand(string,DbParameter[])</c> ；</para>
		/// </remarks>
		public int Update(DataTable dataTable)
		{
			if (dataTable == null)
			{
				throw new ArgumentNullException("dataTable");
			}

			return this.Adapter.Update(dataTable);
		}

		/// <summary>
		/// 为指定 <see cref="DataRow"/> 数组中每个已插入、已更新或已删除的行调用相应的 <b>INSERT</b>、<b>UPDATE</b> 或 <b>DELETE</b> 语句。
		/// </summary>
		/// <param name="rows">用于更新数据源的<see cref="DataRow"/> 数组。</param>
		/// <returns><see cref="DataRow"/> 数组中成功更新的行数。</returns>
		public int Update(DataRow[] rows)
		{
			if (rows == null)
			{
				throw new ArgumentNullException("rows");
			}

			return this.Adapter.Update(rows);
		}
		#endregion

		#region 以下方法从 DataManager 移至这里，主要考虑因素为 Ado.Net 功能的聚合性
		private static System.Text.RegularExpressions.Regex regexParameter = new System.Text.RegularExpressions.Regex(@"@\w+");
		/// <summary>
		/// 对将要执行的命令进行预处理，以确保命令可以在不同的数据库上执行，这通常用于直接对 DbCommand.CommandText 进行赋值的场合。
		/// </summary>
		/// <param name="commandText"></param>
		/// <returns></returns>
		/// <example>
		/// 在下面的情况下，需要使用此方法对命令进行预处理：
		/// this._insertCommand = this.dataManager.Connection.CreateCommand();
		/// this._insertCommand.Connection = this.dataManager.Connection;
		/// this._insertCommand.Transaction = this.dataManager.Transaction;
		/// this._insertCommand.CommandType = System.Data.CommandType.Text;
		/// this._insertCommand.CommandText = this.dataManager.PretreatmentCommandText("INSERT INTO S_MESSAGES(TO_NUMBER,MESSAGE,SEND_TIME,ORG_ID,FLAG) VALUES (@TO_NUMBER,@MESSAGE,@SEND_TIME,@ORG_ID,@FLAG)");
		/// </example>
		internal string PretreatmentCommandText(string commandText)
		{
			if (this.dbProviderFactory.GetType() == typeof(System.Data.SqlClient.SqlClientFactory))
			{
				return commandText;
			}
			return regexParameter.Replace(commandText, "?");
		}

		/// <summary>
		/// 判定指定的命令是否存储过程
		/// </summary>
		/// <param name="commandText"></param>
		/// <returns></returns>
		private static bool IsStoreProcedure(string commandText)
		{
			return commandText.IndexOf(" ") == -1;
		}
		#endregion
	}
}