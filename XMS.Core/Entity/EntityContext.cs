using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Objects;
using System.Data.Entity.ModelConfiguration.Conventions;

using Castle.Core;
using Castle.DynamicProxy;
using System.Linq.Dynamic;

using XMS.Core.Configuration;
using XMS.Core.Logging;
using XMS.Core.Data;

namespace XMS.Core
{
	/// <summary>
	/// 数据库访问接口。
	/// </summary>
	public interface IDatabase
	{
		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，仅返回语句执行影响的行数。
		/// </summary>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>语句执行影响的行数。</returns>
		/// <remarks>
		/// 可以通过此方法执行任意不需要返回数据的 SQL 语句（如 delete、update 等）。
		/// </remarks>
		/// <example>
		/// entityContext.ExecuteNonQuery("UPDATE Person SET Name = @p0 WHERE PersonID = @p1", "Mike", 100);
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		int ExecuteNonQuery(string sql, params object[] parameters);

		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，仅返回语句执行影响的行数。
		/// </summary>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>语句执行影响的行数。</returns>
		/// <remarks>
		/// 可以通过此方法执行任意不需要返回数据的 SQL 语句（如 delete、update 等）。
		/// </remarks>
		int ExecuteNonQuery(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，查询结果以给定的泛型类型返回。
		/// </summary>
		/// <typeparam name="T">要返回的数据的类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>用于执行查询的枚举器。</returns>
		IEnumerable<T> ExecuteQuery<T>(string sql, params object[] parameters);

		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，查询结果以给定的泛型类型返回。
		/// </summary>
		/// <typeparam name="T">要返回的数据的类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>用于执行查询的枚举器。</returns>
		IEnumerable<T> ExecuteQuery<T>(string sql, Dictionary<string, object> parameters);

		#region 事务支持
		/// <summary>
		/// 使用快照隔离级别开始执行事务操作。
		/// SQL Server 中默认不启用事务快照机制，要启用事务快照机制:
		///		1.停掉所有可能正在使用目标数据库的服务和应用程序;
		///		2.如果仍然有连接存在，可通过先分离数据库，在附加数据库的方法断掉连接;
		///		3.在目标数据库中执行以下查询：
		///			ALTER DATABASE xxx SET ALLOW_SNAPSHOT_ISOLATION ON
		///	要在数据库中查看各数据库事务快照机制启用情况，请使用以下语句：
		///		SELECT name, snapshot_isolation_state_desc, is_read_committed_snapshot_on FROM sys.databases
		/// 事务示例：
		/// using(IEntityContext entityContext = this.CreateBusinessContext())
		/// {
		///		entityContext.BeginTransaction();
		///		try
		///		{
		///			// 任意业务代码
		///
		///			entityContext.Commit();
		///		}
		///		catch
		///		{
		///			entityContext.Rollback();
		///			throw;
		///		}
		///	}
		///	备注：
		///		SNAPSHOT 事务与其它事务有一种情况是不一样的，两个事务相继开始，如果两个都是更新同一条记录，那么后面更新的事务
		///		会在前面更新的事务的COMMIT时由等待状态转为抛出错误。或者同样两个事务相继开始，第1个事务在更新这个记录，而第2
		///		个事务在第1个事务 COMMIT 前（不理是在更新语句前还是后）有过查询这条记录的话，那么这第2个事务不管是在第1个事务
		///		COMMIT 前还是后也有更新这条记录的话，那么第2个事务就会抛出并发错误。错误如下所示： “Msg 3960, Level 16, State 2,
		///		Line 2 Snapshot isolation transaction aborted due to update conflict. You cannot use snapshot isolation 
		///		to access table 'dbo.aaa' directly or indirectly in database 'Tecsys_db1' to update, delete, or insert 
		///		the row that has been modified or deleted by another transaction. Retry the transaction or change the 
		///		isolation level for the update/delete statement.”的错误，而其它级别的所有事务则都能正常工作，没有错误发生。
		///		总之，SNAPSHOT事务是两个事务不能同时更新同一条记录，或者是一个事务在更新，另一个事务只要在第1个更新事务 COMMIT
		///		前有过查询，甚至这第2个事务是在第1个事务更新语句后才开始的，这第2个事务一定不能有任何更新这条记录的语句出现，否则就会出错。
		/// </summary>
		void BeginTransaction();

		/// <summary>
		/// 使用指定的事务隔离级别开始执行事务操作。
		/// </summary>
		/// <param name="isolationLevel">用来初始化事务操作的事务隔离级别。</param>
		void BeginTransaction(IsolationLevel isolationLevel);

		/// <summary>
		/// 提交当前事务。
		/// </summary>
		void Commit();

		/// <summary>
		/// 提交当前事务。
		/// </summary>
		void Rollback();
		#endregion

		/// <summary>
		/// 检查当前业务相关的数据库是否存在
		/// </summary>
		/// <returns></returns>
		bool IsDatabaseExists();

		/// <summary>
		/// 检查当前上下文环境中由泛型参数类型指定的实体相关的数据表是否存在。
		/// </summary>
		/// <returns></returns>
		bool IsTableExists<T>();

		/// <summary>
		/// 检查当前上下文环境中指定实体类型相关的数据表是否存在。
		/// </summary>
		/// <returns></returns>
		bool IsTableExists(Type entityType);

		/// <summary>
		/// 创建一个可用于直接操作数据库表的数据表访问适配器。
		/// </summary>
		/// <returns></returns>
		DataTableAdapter CreateDataTableAdapter();
	}

	/// <summary>
	/// 业务上下文接口定义
	/// </summary>
	public interface IBusinessContext
	{
		/// <summary>
		/// 扩展属性。
		/// </summary>
		Dictionary<string, object> ExtendProperties
		{
			get;
		}

		/// <summary>
		/// 获取一个值，该值指示当前业务上下文的运行模式。
		/// </summary>
		RunMode RunMode
		{
			get;
		}

		/// <summary>
		/// 获取适用于当前业务上下文的应用代理。
		/// </summary>
		AppAgent AppAgent
		{
			get;
		}

		/// <summary>
		/// 根据当前运行模式，自动创建一个适用于当前运行模式的实体上下文。
		/// </summary>
		/// <returns></returns>
		IEntityContext CreateEntityContext();
	}

	/// <summary>
	/// 实体上下文接口定义
	/// </summary>
	public interface IEntityContext : IDatabase, IDisposable
	{
		/// <summary>
		/// 获取当前实体上下文相关的业务上下文对象。
		/// </summary>
		IBusinessContext BusinessContext
		{
			get;
		}

		/// <summary>
		/// 为指定的实体类型获取其映射的物理表名。
		/// </summary>
		/// <param name="entityType">要获取其映射的物理表名的实体类型。</param>
		/// <returns>实体类型映射的物理表名。</returns>
		string GetMappingToTable(Type entityType);

		/// <summary>
		/// 为指定的实体类型获取其映射的物理表名。
		/// </summary>
		/// <typeparam name="T">要获取其映射的物理表名的实体类型。</typeparam>
		/// <returns>实体类型映射的物理表名。</returns>
		string GetMappingToTable<T>();

		/// <summary>
		/// 根据指定的表名获取其物理分区表名，该方法可用于获取那些定义了实体模型的表的分区表名，也可以获取那些不具有实体模型定义的表的分区表名。
		/// </summary>
		/// <param name="rawTableName">原表名。</param>
		/// <returns>分区表名</returns>
		string GetPartitionTableName(string rawTableName);

		/// <summary>
		/// 添加实体。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Entity"></param>
		void Add<T>(T Entity) where T : class;

		/// <summary>
		/// 删除实体。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Entity"></param>
		void Delete<T>(T Entity) where T : class;

		/// <summary>
		/// 更新实体。
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="Entity"></param>
		void Update<T>(T Entity) where T : class;

		/// <summary>
		/// 添加或更新实体，该方法当指定的实体具有健值时对数据库执行一次查询以判断目标数据是否确实存在，如果存在，则执行更新操作，其它情况下执行添加操作。
		/// </summary>
		/// <typeparam name="T">实体的类型。</typeparam>
		/// <param name="entity">要添加或更新的实体。</param>
		void AddOrUpdate<T>(T entity) where T : class;

		T FindByPrimaryKey<T>(params object[] keyValues) where T : class;

		List<T> FindByProperty<T>(string property, object value) where T : class;

		List<T> FindByAll<T>(List<KeyValuePair<string, object>> propertyValues) where T : class;

		List<T> FindByAny<T>(List<KeyValuePair<string, object>> propertyValues) where T : class;

		IQueryable<T> GetQueryableDataSource<T>() where T : class;

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的第一条数据对应的对象实例。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>满足条件的第一条数据对应的对象实例。</returns>
		/// <example>
		/// entityContext.ExecuteScalar&lt;string&gt;("select Name from Person where PersonID=@p0", 100);</String>
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		T ExecuteScalar<T>(string sql, params object[] parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的第一条数据对应的对象实例。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的第一条数据对应的对象实例。</returns>
		T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的对象的集合。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>满足条件的对象的集合。</returns>
		List<T> ExecuteList<T>(string sql, params object[] parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的对象的集合。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 sql 参数指定的语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		List<T> ExecuteList<T>(string sql, Dictionary<string, object> parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">用于参数化 condition 参数指定的语句的命令参数。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		List<T> ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, params object[] parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">与 condition 参数指定的语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		List<T> ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="tableName">要查询表名。</param>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">与 condition 参数指定的语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		List<T> ExecuteList<T>(string tableName, string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行分页查询，该接口仅用于提供对现有 SQLHelper 组件的兼容性。
		/// </summary>
		/// <typeparam name="T">回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="pageIndex">页码。</param>
		/// <param name="pageSize">页大小。</param>
		/// <param name="parameters">用于参数化 condition 参数指定的语句的命令参数。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecutePagedList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		[Obsolete("该方法已经过时，当前版本中仅用于提供的已有项目的兼容性，将来可能不会继续提供支持，请使用 ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, params object[] parameters) 代替。")]
		List<T> ExecutePagedList<T>(string fields, string condition, string orderBy, int pageIndex, int pageSize, params object[] parameters);
	
		/// <summary>
		/// 在当前实体上下文相关的数据库中执行分页查询，该接口仅用于提供对现有 SQLHelper 组件的兼容性。
		/// </summary>
		/// <typeparam name="T">回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="pageIndex">页码。</param>
		/// <param name="pageSize">页大小。</param>
		/// <param name="parameters">与 condition 参数指定的语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		[Obsolete("该方法已经过时，当前版本中仅用于提供的已有项目的兼容性，将来可能不会继续提供支持，请使用 ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters) 代替。")]
		List<T> ExecutePagedList<T>(string fields, string condition, string orderBy, int pageIndex, int pageSize, Dictionary<string, object> parameters);

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行分页查询，该接口仅用于提供对现有 SQLHelper 组件的兼容性。
		/// </summary>
		/// <typeparam name="T">回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="tableName">要查询表名。</param>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="pageIndex">页码。</param>
		/// <param name="pageSize">页大小。</param>
		/// <param name="parameters">与 condition 参数指定的语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		[Obsolete("该方法已经过时，当前版本中仅用于提供的已有项目的兼容性，将来可能不会继续提供支持，请使用 ExecuteList<T>(string tableName, string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters) 代替。")]
		List<T> ExecutePagedList<T>(string tableName, string fields, string condition, string orderBy, int pageIndex, int pageSize, Dictionary<string, object> parameters);
	}

	/// <summary>
	/// 业务上下文基类。
	/// </summary>
	public abstract class BusinessContextBase : IBusinessContext
	{
		private Dictionary<string, object> extendProperties = null;

		/// <summary>
		/// 扩展属性。
		/// </summary>
		public Dictionary<string, object> ExtendProperties
		{
			get
			{
				if (extendProperties == null)
				{
					extendProperties = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
				}
				return extendProperties;
			}
		}

		/// <summary>
		/// 初始化 BusinessContextBase 类的新实例。
		/// </summary>
		protected BusinessContextBase()
		{
		}

		/// <summary>
		/// 获取一个值，该值指示当前业务上下文的运行模式。
		/// </summary>
		public RunMode RunMode
		{
			get
			{
				return RunContext.Current.RunMode;
			}
		}

		/// <summary>
		/// 获取适用于当前业务上下文的客户端应用代理。
		/// </summary>
		public AppAgent AppAgent
		{
			get
			{
				return SecurityContext.Current.AppAgent;
			}
		}

		/// <summary>
		/// 根据当前运行模式，自动创建一个适用于当前运行模式的实体上下文。
		/// </summary>
		/// <returns>新创建的实体上下文。</returns>
		public virtual IEntityContext CreateEntityContext()
		{
			// 自动匹配运行模式的情况下，不需要向 ThreadContext 中写值。
			return this.CreateEntityContext(this.RunMode);
		}

		/// <summary>
		/// 根据指定的运行模式创建实体上下文。
		/// </summary>
		/// <param name="runMode">用于创建实体上下文的运行模式。</param>
		/// <returns>新创建的实体上下文。</returns>
		protected abstract IEntityContext CreateEntityContext(RunMode runMode);
	}
}

namespace XMS.Core.Entity
{
	/// <summary>
	/// 数据库业务上下文基类。
	/// </summary>
	public abstract class DbBusinessContextBase : BusinessContextBase
	{
		private class InternalConnectionString
		{
			public readonly string RawConnectionString;

			public readonly string ServerName;

			public readonly string DatabaseName;

			public readonly System.Data.Common.DbProviderFactory DBProviderFactory;

			public InternalConnectionString(string rawConnectionString, string serverName, string databaseName, System.Data.Common.DbProviderFactory dbProviderFactory)
			{
				this.RawConnectionString = rawConnectionString;
				this.ServerName = serverName;
				this.DatabaseName = databaseName;
				this.DBProviderFactory = dbProviderFactory;
			}

			public string GetConnectionString(RunMode runMode)
			{
				return runMode != RunMode.Release ? this.RawConnectionString.Replace(this.DatabaseName, this.DatabaseName + "_" + runMode.ToString()) : this.RawConnectionString;
			}

			/// <summary>
			/// 检查当前业务相关的数据库是否存在
			/// </summary>
			/// <returns></returns>
			public bool IsDatabaseExists(RunMode runMode)
			{
				return Database.Exists( this.GetConnectionString(runMode) );
			}

			private static Dictionary<string, Dictionary<string, HashSet<string>>> dbServers_release = new Dictionary<string, Dictionary<string, HashSet<string>>>(2, StringComparer.InvariantCultureIgnoreCase);
			private static Dictionary<string, Dictionary<string, HashSet<string>>> dbServers_demo = new Dictionary<string, Dictionary<string, HashSet<string>>>(2, StringComparer.InvariantCultureIgnoreCase);

			private bool schema_tables_inited_release = false;
			private bool schema_tables_inited_demo = false;

			public void RestSchemas()
			{
				this.schema_tables_inited_demo = false;
				this.schema_tables_inited_release = false;

				dbServers_release = new Dictionary<string, Dictionary<string, HashSet<string>>>(2, StringComparer.InvariantCultureIgnoreCase);
				dbServers_demo = new Dictionary<string, Dictionary<string, HashSet<string>>>(2, StringComparer.InvariantCultureIgnoreCase);
			}

			public void InitSchemas(RunMode runMode)
			{
				if (runMode != RunMode.Release)
				{
					if (!schema_tables_inited_demo)
					{
						lock (dbServers_demo)
						{
							if (!schema_tables_inited_demo)
							{
								using (DbConnection connection = this.DBProviderFactory.CreateConnection())
								{
									connection.ConnectionString = this.RawConnectionString.Replace(this.DatabaseName, this.DatabaseName + "_" + runMode.ToString());

									connection.Open();

									try
									{
										InitDatabaseSchema(this, connection, dbServers_demo);
									}
									finally
									{
										connection.Close();
									}
								}

								schema_tables_inited_demo = true;
							}
						}
					}
				}
				else
				{
					if (!schema_tables_inited_release)
					{
						lock (dbServers_release)
						{
							if (!schema_tables_inited_release)
							{
								using (DbConnection connection = this.DBProviderFactory.CreateConnection())
								{
									connection.ConnectionString = this.RawConnectionString;

									connection.Open();

									try
									{
										InitDatabaseSchema(this, connection, dbServers_release);
									}
									finally
									{
										connection.Close();
									}
								}

								schema_tables_inited_release = true;
							}
						}
					}
				}
			}

			private static void InitDatabaseSchema(InternalConnectionString connStr, DbConnection connection, Dictionary<string, Dictionary<string, HashSet<string>>> dbServers)
			{
				Dictionary<string, HashSet<string>> dbDatabases = null;
				HashSet<string> dbTables = null;

				dbDatabases = dbServers.ContainsKey(connStr.ServerName) ? dbServers[connStr.ServerName] : null;

				if (dbDatabases == null)
				{
					dbDatabases = new Dictionary<string, HashSet<string>>(2, StringComparer.InvariantCultureIgnoreCase);

					dbServers.Add(connStr.ServerName, dbDatabases);
				}

				dbTables = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

				DataTable schemas = connection.GetSchema("Tables");

				if (schemas != null)
				{
					for (int i = 0; i < schemas.Rows.Count; i++)
					{
						dbTables.Add(schemas.Rows[i]["TABLE_NAME"].ToString());
					}
				}

				dbDatabases.Add(connStr.DatabaseName, dbTables);
			}

			public void HandleTableCreated(string tableName, RunMode runMode)
			{
				if (String.IsNullOrEmpty(tableName))
				{
					throw new ArgumentNullException("tableName");
				}

				Dictionary<string, HashSet<string>> dbDatabases = null;
				HashSet<string> dbTables = null;

				if (runMode != RunMode.Release)
				{
					if (schema_tables_inited_demo)
					{
						dbDatabases = dbServers_demo.ContainsKey(this.ServerName) ? dbServers_demo[this.ServerName] : null;
					}
				}
				else
				{
					if (schema_tables_inited_release)
					{
						dbDatabases = dbServers_release.ContainsKey(this.ServerName) ? dbServers_release[this.ServerName] : null;
					}
				}

				if (dbDatabases != null)
				{
					dbTables = dbDatabases.ContainsKey(this.DatabaseName) ? dbDatabases[this.DatabaseName] : null;
				}

				if (dbTables != null)
				{
					lock (dbTables)
					{
						if (!dbTables.Contains(tableName))
						{
							dbTables.Add(tableName);
						}
					}
				}
			}

			/// <summary>
			/// 检查当前上下文环境中指定实体类型相关的数据表是否存在。
			/// </summary>
			/// <returns></returns>
			public bool IsTableExists(string tableName, RunMode runMode)
			{
				if (String.IsNullOrEmpty(tableName))
				{
					return false;
				}

				Dictionary<string, HashSet<string>> dbDatabases = null;
				HashSet<string> dbTables = null;

				if (runMode != RunMode.Release)
				{
					if (!schema_tables_inited_demo)
					{
						this.InitSchemas(runMode);
					}

					dbDatabases = dbServers_demo.ContainsKey(this.ServerName) ? dbServers_demo[this.ServerName] : null;
				}
				else
				{
					if (!schema_tables_inited_release)
					{
						this.InitSchemas(runMode);
					}

					dbDatabases = dbServers_release.ContainsKey(this.ServerName) ? dbServers_release[this.ServerName] : null;
				}

				if (dbDatabases != null)
				{
					dbTables = dbDatabases.ContainsKey(this.DatabaseName) ? dbDatabases[this.DatabaseName] : null;
				}

				if (dbTables != null)
				{
					lock (dbTables)
					{
						return dbTables.Contains(tableName);
					}
				}

				return false;
			}
		}

		private class ConnectionStringManager
		{
			// .NET Framework Data Provider for Oracle 连接字符串参考写法，参见 http://www.connectionstrings.com/oracle
			// Standard：							Data Source=MyOracleDB;Integrated Security=yes;
			// Specifying username and password：	Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;Integrated Security=no;
			// Omiting tnsnames.ora：				SERVER=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));uid=myUsername;pwd=myPassword;
			//						or ：			Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;	
			private static Regex regOracleConnStr = new System.Text.RegularExpressions.Regex(@"^.*(\s*Data\s+Source|SERVER)\s*=\s*((.*\(\s*SERVICE_NAME\s*=\s*([^\)]*)\s*\))|([^;]*)).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			// SQL Server 连接字符串参考写法，参见 参见 http://www.connectionstrings.com/sql-server-2008
			// Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;
			// Data Source=myServerAddress;Initial Catalog=myDataBase;Integrated Security=SSPI;
			private static Regex regSqlServerConnStr = new System.Text.RegularExpressions.Regex(@"^.*(\s*Initial\s+Catalog|Database)\s*=\s*([^;]*).*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			private static Regex regSqlServerKeyValues = new Regex(@"(?<key>(\w+)(\s+(\w+))*)\s*=\s*(?<value>[^;]+)");

			private static Dictionary<string, InternalConnectionString> connections = new Dictionary<string, InternalConnectionString>(StringComparer.InvariantCultureIgnoreCase);

			public static InternalConnectionString GetConnectionString(string rawConnectionString)
			{
				if (String.IsNullOrEmpty(rawConnectionString))
				{
					return null;
				}
				lock (connections)
				{
					InternalConnectionString connectionString = null;
					string serverName = null;
					string databaseName = null;

					if (connections.ContainsKey(rawConnectionString))
					{
						connectionString = connections[rawConnectionString];
					}
					else
					{
						Match match = regSqlServerConnStr.Match(rawConnectionString);

						if (match.Success)
						{
							if (!String.IsNullOrWhiteSpace(match.Groups[2].Value))
							{
								MatchCollection matchs = regSqlServerKeyValues.Matches(rawConnectionString);
								foreach (Match m in matchs)
								{
									switch (m.Groups["key"].Value.ToLower())
									{
										case "initial catalog":
										case "database":
											databaseName = m.Groups["value"].Value.DoTrim();
											break;
										case "data source":
										case "server":
											serverName = m.Groups["value"].Value.DoTrim();
											break;
										default:
											break;
									}
								}
								if (!String.IsNullOrEmpty(serverName) && !String.IsNullOrEmpty(databaseName))
								{
									connectionString = new InternalConnectionString(rawConnectionString, serverName, databaseName, System.Data.SqlClient.SqlClientFactory.Instance);
								}
							}
						}

						if (connectionString == null)
						{
							// 暂不支持 Oracle，因为 System.Data.OracleClient 中的类型在 .net 4.0 中已被微软声明为过时，在以后的版本中可能会被删除，Microsoft 建议使用第三方 Oracle 提供程序。
							//match = regOracleConnStr.Match(connectionString);
							//if(match.Success)
							//{
							//    if(!String.IsNullOrWhiteSpace( match.Groups[4].Value ))
							//    {
							//        this.databaseName = match.Groups[4].Value;
							//        this.dbProviderFactory = System.Data.OracleClient.OracleClientFactory.Instance;
							//    }
							//    else if(!String.IsNullOrWhiteSpace(match.Groups[2].Value))
							//    {
							//        this.databaseName = match.Groups[2].Value;
							//        this.dbProviderFactory = System.Data.OracleClient.OracleClientFactory.Instance;
							//    }
							//}
						}

						if (connectionString != null)
						{
							connections.Add(rawConnectionString, connectionString);
						}
					}

					return connectionString;
				}
			}
		}

		private InternalConnectionString connectionString;

		internal System.Data.Common.DbProviderFactory dbProviderFactory
		{
			get
			{
				return this.connectionString.DBProviderFactory;
			}
		}

		/// <summary>
		/// 获取或设置当前业务上下文相关的连接字符串
		/// </summary>
		public string ConnectionString
		{
			get
			{
				return this.connectionString.RawConnectionString;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value != this.connectionString.RawConnectionString)
				{
					this.InitConnectionString(value);
				}
			}
		}

		/// <summary>
		/// 获取或设置数据库的名称。
		/// </summary>
		protected string ServerName
		{
			get
			{
				return this.connectionString.ServerName;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value != this.connectionString.ServerName)
				{
					this.InitConnectionString(this.connectionString.RawConnectionString.Replace(this.connectionString.ServerName, value));
				}
			}
		}

		/// <summary>
		/// 获取或设置数据库的名称。
		/// </summary>
		protected string DatabaseName
		{
			get
			{
				return this.connectionString.DatabaseName;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				if (value != this.connectionString.DatabaseName)
				{
					this.InitConnectionString(this.connectionString.RawConnectionString.Replace(this.connectionString.DatabaseName, value));
				}
			}
		}

		// 分库时不需要重写此方法
		// 单表分区时需要重写此方法
		/// <summary>
		/// 获取用于对表进行水平分区的键，在对表进行水平分区时，该分区键决定了分区表的表名，仅在需要对表进行水平分区时重写此方法，其它情况下，忽略此方法。
		/// </summary>
		/// <value>用于分区的键。</value>
		protected virtual string TablePartitionKey
		{
			get
			{
				return String.Empty;
			}
		}

		/// <summary>
		/// 当表不存在时是否调用 CreateTable 方法创建表
		/// </summary>
		private bool createDatabaseOrTableWhenNotExists = true;


		private string entityContextKey = null;

		// EntityContextKey 和库名、表分区键有关
		private string EntityContextKey
		{
			get
			{
				if (this.entityContextKey == null)
				{
					string tablePartitionKey = this.TablePartitionKey;

					this.entityContextKey = this.GetType().FullName + "_" + this.ServerName + "_" + this.DatabaseName + (String.IsNullOrEmpty(tablePartitionKey) ?  String.Empty : "_" + tablePartitionKey);
				}
				return this.entityContextKey;
			}
		}

		/// <summary>
		/// 初始化数据业务上下文的实例。
		/// </summary>
		/// <param name="nameOrConnectionString"></param>
		public DbBusinessContextBase(string nameOrConnectionString)
			: base()
		{
			this.InitConnectionString(nameOrConnectionString);
		}

		/// <summary>
		/// 初始化数据业务上下文的实例。
		/// </summary>
		/// <param name="nameOrConnectionString"></param>
		/// <param name="createDatabaseOrTableWhenNotExists">指示在分区表不存在时是否应该创建表， true 创建， false 不创建并抛出异常。</param>
		protected DbBusinessContextBase(string nameOrConnectionString, bool createDatabaseOrTableWhenNotExists)
			: base()
		{
			this.InitConnectionString(nameOrConnectionString);

			this.createDatabaseOrTableWhenNotExists = createDatabaseOrTableWhenNotExists;
		}


		private void InitConnectionString(string nameOrConnectionString)
		{
			if (String.IsNullOrWhiteSpace(nameOrConnectionString))
			{
				throw new ArgumentNullOrWhiteSpaceException("nameOrConnectionString");
			}

			this.connectionString = ConnectionStringManager.GetConnectionString(nameOrConnectionString);
			if (this.connectionString == null)
			{
				this.connectionString = ConnectionStringManager.GetConnectionString(XMS.Core.Container.ConfigService.GetConnectionString(nameOrConnectionString));
			}

			if (this.connectionString == null)
			{
				throw new ArgumentException(String.Format("未找到指定名称为 {0} 的连接字符串或者连接字符串的格式不正确。", nameOrConnectionString));
			}

			this.entityContextKey = null;
		}

		/// <summary>
		/// 获取指定原表名对应的分区表名，默认不对表进行分区的情况下，直接返回 <see cref="rawTableName"/>。
		/// </summary>
		/// <param name="rawTableName">要获取其分区表名的原表名。</param>
		/// <returns>与指定原表名对应的分区表名。</returns>
		/// <remarks>
		/// 对继承者的说明：当需要使用 DataTableAdapter 的方式访问数据且需要对单表进行分区时需要重载此方法以确定在当前业务上下文环境中要访问的目标表名。
		/// </remarks>
		protected internal virtual string GetPartitionTableName(string rawTableName)
		{
			return rawTableName;
		}

		/// <summary>
		/// 获取当前数据业务上下文中模型映射的集合（即模型到数据库表的映射)
		/// </summary>
		/// <returns></returns>
		public abstract Dictionary<Type, string> GetModelMappings();

		#region IsDatabaseExists、IsTableExists、CreateTable
		/// <summary>
		/// 检查当前业务相关的数据库是否存在
		/// </summary>
		/// <returns></returns>
		internal bool IsDatabaseExists(RunMode runMode)
		{
			return this.connectionString.IsDatabaseExists(runMode);
		}

		internal void HandleTableCreated(string tableName, RunMode runMode)
		{
			this.connectionString.HandleTableCreated(tableName, runMode);
		}

		/// <summary>
		/// 检查当前上下文环境中指定实体类型相关的数据表是否存在。
		/// </summary>
		/// <returns></returns>
		internal bool IsTableExists(string tableName, RunMode runMode)
		{
			return this.connectionString.IsTableExists(tableName, runMode);
		}

		/// <summary>
		/// 创建表。
		/// </summary>
		/// <param name="db"></param>
		/// <param name="tableName"></param>
		/// <param name="entityType"></param>
		internal protected virtual void CreateTable(IDatabase db, string tableName, Type entityType)
		{
			throw new NotSupportedException();
		}
		#endregion

		private static Dictionary<string, ProxyGenerator> generators_release = new Dictionary<string, ProxyGenerator>();
		private static Dictionary<string, ProxyGenerator> generators_demo = new Dictionary<string, ProxyGenerator>();

		/// <summary>
		/// 根据指定的运行模式创建实体上下文。
		/// </summary>
		/// <param name="runMode">用于创建实体上下文的运行模式。</param>
		/// <returns>新创建的实体上下文。</returns>
		protected override IEntityContext CreateEntityContext(RunMode runMode)
		{
			string entityContextKey = this.EntityContextKey;

			Dictionary<string, ProxyGenerator> generators = runMode != Core.RunMode.Release ? generators_demo : generators_release;

			ProxyGenerator generator = null;
			if (!generators.ContainsKey(entityContextKey))
			{
				lock (generators)
				{
					if (!generators.ContainsKey(entityContextKey))
					{
						generator = new ProxyGenerator();
						generators.Add(entityContextKey, generator);
					}
				}
			}
			generator = generators[entityContextKey];

			InternalDbContext dbContext = (InternalDbContext)generator.CreateClassProxy(typeof(InternalDbContext),
				new object[] { this, this.connectionString.GetConnectionString(runMode), runMode },
				new DbContextInterceptor());

			return new DbEntityContext(this, dbContext);
		}

		
		/// <summary>
		/// 该类由实体访问模块内部使用，请不要直接使用。
		/// </summary>
		public class InternalDbContext : DbContext, IDatabase
		{
			private DbBusinessContextBase businessContext;

			/// <summary>
			/// BusinessContext
			/// </summary>
			public DbBusinessContextBase BusinessContext
			{
				get
				{
					return this.businessContext;
				}
			}

			private string connectionString = null;

			private RunMode runMode;

			/// <summary>
			/// 获取当前内部 DbContext 的运行模式。
			/// </summary>
			public RunMode RunMode
			{
				get
				{
					return this.runMode;
				}
			}

			/// <summary>
			/// 初始化 InternalDbContext 类的新实例。
			/// </summary>
			/// <param name="businessContext">businessContext</param>
			/// <param name="connectionString">connectionString</param>
			/// <param name="runMode">runMode</param>
			public InternalDbContext(DbBusinessContextBase businessContext, string connectionString, RunMode runMode)
				: base(connectionString)
			{
				this.connectionString = connectionString;

				this.businessContext = businessContext;

				this.runMode = runMode;
			}

			internal InternalDbContext(InternalDbContext context)
				: base(context.connectionString)
			{
				this.businessContext = context.businessContext;

				this.runMode = context.RunMode;
			}

			/// <summary>
			/// 保存更改
			/// </summary>
			/// <returns></returns>
			public override int SaveChanges()
			{
				try
				{
					return base.SaveChanges();
				}
				// 并发异常直接抛出
				catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
				{
					throw;
				}
				// 其它异常如果发现异常的内部异常的错误码为 3960 并发错误， 则抛出并发异常，否则，抛出原始异常
				catch (Exception err)
				{
					Exception currentException = err;
					System.Data.SqlClient.SqlException sqlErr = null;
					do
					{
						sqlErr = currentException as System.Data.SqlClient.SqlException;
						if (sqlErr != null)
						{
							if (sqlErr.Number == 3960)
							{
								throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
							}
							break;
						}
						currentException = currentException.InnerException;
					}
					while (currentException != null);

					throw;
				}
			}

			/// <summary>
			/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，
			/// </summary>
			/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
			/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
			/// <returns>语句执行影响的行数。</returns>
			/// <remarks>
			/// 可以通过此方法执行任意不需要返回数据的 SQL 语句（如 delete、update 等）。
			/// </remarks>
			/// <example>
			/// entityContext.ExecuteNonQuery("UPDATE Person SET Name = @p0 WHERE PersonID = @p1", "Mike", 100);
			/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
			/// </example>
			public int ExecuteNonQuery(string sql, params object[] parameters)
			{
				return this.Database.ExecuteSqlCommand(sql, parameters);
			}

			/// <summary>
			/// 在数据库中直接执行指定的 SQL 语句，查询结果以给定的泛型类型返回。
			/// </summary>
			/// <typeparam name="T">要返回的数据的类型。</typeparam>
			/// <param name="sql">要执行的 SQL 语句。</param>
			/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
			/// <returns>用于执行查询的枚举器。</returns>
			public IEnumerable<T> ExecuteQuery<T>(string sql, params object[] parameters)
			{
				return this.Database.SqlQuery<T>(sql, parameters);
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <param name="sql"></param>
			/// <param name="parameters"></param>
			/// <returns></returns>
			public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="sql"></param>
			/// <param name="parameters"></param>
			/// <returns></returns>
			public IEnumerable<T> ExecuteQuery<T>(string sql, Dictionary<string, object> parameters)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			public void BeginTransaction()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <param name="isolationLevel"></param>
			public void BeginTransaction(IsolationLevel isolationLevel)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			public void Commit()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			public void Rollback()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <returns></returns>
			public bool IsDatabaseExists()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public bool IsTableExists<T>()
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <param name="entityType"></param>
			/// <returns></returns>
			public bool IsTableExists(Type entityType)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// 该方法不被支持。
			/// </summary>
			/// <returns></returns>
			public DataTableAdapter CreateDataTableAdapter()
			{
				throw new NotImplementedException();
			}
		}

		private class DbContextInterceptor : IInterceptor
		{
			public void Intercept(IInvocation invocation)
			{
				// 当拦截的方法是 OnModelCreating 时
				if (invocation.Method.Name == "OnModelCreating")
				{
					// 获取实体上下文
					InternalDbContext simpleDbContext = (InternalDbContext)invocation.InvocationTarget;

					// 通过模型构建起将模型映射集合中的映射进行初始化
					DbModelBuilder modelBuilder = (DbModelBuilder)invocation.Arguments[0];

					// 移除模型（ModeHash）验证
					modelBuilder.Conventions.Remove<System.Data.Entity.Infrastructure.IncludeMetadataConvention>();

					// 通过反射初始化
					// 代替：Database.SetInitializer<MyDomainContext>(new CreateDatabaseIfNotExists<MyDomainContext>())
					Type databaseType = typeof(Database);
					Type databaseInitializerFactoryType = typeof(CreateDatabaseIfNotExists<>).MakeGenericType(invocation.TargetType);
					MethodInfo setInitializerMethod = databaseType.GetMethod("SetInitializer", BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(invocation.TargetType);
					setInitializerMethod.Invoke(null, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static, null, new object[]{
						Activator.CreateInstance(databaseInitializerFactoryType)
					}, null);

					#region 通过配置文件将实体和表进行映射
					// 根据业务上下文获取模型映射集合
					Dictionary<Type, string> modelMappings = simpleDbContext.BusinessContext.GetModelMappings();

					foreach (KeyValuePair<Type, string> kvp in modelMappings)
					{
						//modelBuilder.Entity<Type>().ToTable(kvp.Value);
						MethodInfo entityMethod = typeof(DbModelBuilder).GetMethod("Entity", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(kvp.Key);
						Type entityConfigurationType = typeof(System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<>).MakeGenericType(kvp.Key);

						object entityTypeConfiguration = entityMethod.Invoke(modelBuilder, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, null, null);

						entityConfigurationType.InvokeMember("ToTable", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, entityTypeConfiguration, new object[]{
							kvp.Value
						});
					}
					#endregion

					// 在数据库存在的情况下，创建分区表
					using (InternalDbContext dbContextForDdl = new InternalDbContext(simpleDbContext))
					{
						if (!simpleDbContext.BusinessContext.createDatabaseOrTableWhenNotExists)
						{
							if (!simpleDbContext.BusinessContext.IsDatabaseExists(simpleDbContext.RunMode))
							{
								throw new DatabaseNotExistException(dbContextForDdl.Database.Connection.Database);
							}
						}

						if (simpleDbContext.BusinessContext.IsDatabaseExists(simpleDbContext.RunMode))
						{
							foreach (KeyValuePair<Type, string> kvp in modelMappings)
							{
								// 建表不成功时，重新初始化 Schema，并重试一次
								int retryCount = 0;

								while (true)
								{
									if (!simpleDbContext.BusinessContext.IsTableExists(kvp.Value, simpleDbContext.RunMode))
									{
										if (!simpleDbContext.BusinessContext.createDatabaseOrTableWhenNotExists)
										{
											throw new DataTableNotExistException(dbContextForDdl.Database.Connection.Database, kvp.Value);
										}

										try
										{
											simpleDbContext.BusinessContext.CreateTable(dbContextForDdl, kvp.Value, kvp.Key);
											simpleDbContext.BusinessContext.HandleTableCreated(kvp.Value, simpleDbContext.RunMode);
										}
										catch (NotSupportedException)
										{
											throw new NotSupportCreateDataTableException(dbContextForDdl.Database.Connection.Database, kvp.Value);
										}
										catch
										{
											simpleDbContext.BusinessContext.connectionString.RestSchemas();

											retryCount++;

											if (retryCount <= 1)
											{
												continue;
											}

											throw;
										}
									}
									break;
								}
							}
						}
					}
				}
				invocation.Proceed();
			}
		}
	}

	//DbEntityContext、XmlEntityContext...
	/// <summary>
	/// 数据实体访问上下文。
	/// </summary>
	public sealed class DbEntityContext : IEntityContext, IDisposable
	{
		private DbBusinessContextBase businessContext = null;
		/// <summary>
		/// 获取当前实体上下文相关的业务上下文。
		/// </summary>
		public IBusinessContext BusinessContext
		{
			get
			{
				return this.businessContext;
			}
		}

		private DbBusinessContextBase.InternalDbContext dbContext;
		internal Database database;

		/// <summary>
		/// 初始化 DbEntityContext 类的新实例。
		/// </summary>
		/// <param name="businessContext">业务上下文</param>
		/// <param name="dbContext">数据上下文</param>
		internal DbEntityContext(DbBusinessContextBase businessContext, DbBusinessContextBase.InternalDbContext dbContext)
		{
			this.businessContext = businessContext;

			this.dbContext = dbContext;
			this.database = dbContext.Database;
		}

		private DbEntityContext()
		{
		}

		#region 事物支持
		// 事务示例：
		/* 应换用以下方式提供支持
 * 如果需要通过事务保护一段代码，在 IEntityContext 是由外部传递的情况下，任意情况下都不需要考虑
 * 外部是否对 IEntityContext 调用了事务支持
 * 比如下面的示例，方法 B和A中分别启用了事务，并且方法 A 调用了方法 B，对于方法 B 而言，如果是单独调用，则事务开始和结束与 B 内部，如果通过 A 调用
 * B，则事务开始和结束与 A 内部，B 中的事务将被忽略。:
 * public void B(IEntityContext entityContext)
 * {
 *		entityContext.BeginTransaction();
 *		try
 *		{
 *			...etc..
 * 
 *			entityContext.Commit();
 *		}
 *		catch
 *		{
 *			entityContext.Rollback();
 *			throw;
 *		}
 * }
 * 
 * public void A(IEntityContext entityContext)
 * {
 *		entityContext.BeginTransaction();
 *		try
 *		{
 *			B(entityContext);
 *			...etc..
 * 
 *			entityContext.Commit();
 *		}
 *		catch
 *		{
 *			entityContext.Rollback();
 *			throw;
 *		}
 * }
 */
		private int transactionDepth = 0;
		private ObjectContext objectContext;
		internal DbTransaction transaction;

		public bool InTransaction
		{
			get
			{
				return this.transactionDepth > 0 && this.transaction != null;
			}
		}

		public void BeginTransaction()
		{
			// 启用快照隔离
			this.BeginTransaction(IsolationLevel.Snapshot);
		}

		public void BeginTransaction(IsolationLevel isolationLevel)
		{
			if (transactionDepth <= 0)
			{
				if (this.transaction == null)
				{
					if (this.objectContext == null)
					{
						this.objectContext = ((System.Data.Entity.Infrastructure.IObjectContextAdapter)this.dbContext).ObjectContext;

						this.objectContext.ContextOptions.ProxyCreationEnabled = false;
					}

					ConnectionState connectionState = this.objectContext.Connection.State;

					if (((connectionState | ConnectionState.Closed) == ConnectionState.Closed) || ((connectionState | ConnectionState.Broken) == ConnectionState.Broken))
					{
						this.objectContext.Connection.Close();

						this.objectContext.Connection.Open();
					}

					this.transaction = this.objectContext.Connection.BeginTransaction(isolationLevel);
				}
			}
			transactionDepth++;
		}

		public void Commit()
		{
			if (transactionDepth > 1)
			{
				transactionDepth--;
			}
			else
			{
				if (transactionDepth > 0)
				{
					transactionDepth--;
				}
				if (this.transaction != null)
				{
					try
					{
						this.transaction.Commit();
					}
					catch
					{
						throw;
					}
					
					this.transaction.Dispose();
					this.transaction = null;
				}
			}
		}

		public void Rollback()
		{
			if (transactionDepth > 1)
			{
				transactionDepth--;
			}
			else
			{
				if (transactionDepth > 0)
				{
					transactionDepth--;
				}

				if (this.transaction != null)
				{
					try
					{
						this.transaction.Rollback();
					}
					catch
					{
						throw;
					}

					this.transaction.Dispose();
					this.transaction = null;
				}
			}
		}

		#endregion

		/// <summary>
		/// 为指定的实体类型获取其映射的物理表名。
		/// </summary>
		/// <param name="entityType">要获取其映射的物理表名的实体类型。</param>
		/// <returns>实体类型映射的物理表名。</returns>
		public string GetMappingToTable(Type entityType)
		{
			Dictionary<Type, string> modelMappings = this.businessContext.GetModelMappings();
			if (!modelMappings.ContainsKey(entityType))
			{
				object[] attributes = entityType.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.TableAttribute), true);
				if (attributes != null && attributes.Length > 0)
				{
					return ((TableAttribute)attributes[0]).Name;
				}
				return entityType.Name;
			}
			return modelMappings[entityType];
		}

		/// <summary>
		/// 为指定的实体类型获取其映射的物理表名。
		/// </summary>
		/// <typeparam name="T">要获取其映射的物理表名的实体类型。</typeparam>
		/// <returns>实体类型映射的物理表名。</returns>
		public string GetMappingToTable<T>()
		{
			return this.GetMappingToTable(typeof(T));
		}

		/// <summary>
		/// 根据指定的表名获取其物理分区表名，该方法可用于获取那些定义了实体模型的表的分区表名，也可以获取那些不具有实体模型定义的表的分区表名。
		/// </summary>
		/// <param name="rawTableName">原表名。</param>
		/// <returns>分区表名</returns>
		public string GetPartitionTableName(string rawTableName)
		{
			return this.businessContext.GetPartitionTableName(rawTableName);
		}

		public void Add<T>(T entity) where T : class
		{
			this.dbContext.Set<T>().Add(entity);

			this.dbContext.SaveChanges();
		}

		public void Delete<T>(T entity) where T : class
		{
			// 先从 local 中查找 entity 对应的实体是否已加载
			T localEntity = null;
			if (this.dbContext.Set<T>().Local.Count > 0)
			{
				for (int i = 0; i < this.dbContext.Set<T>().Local.Count; i++)
				{
					if (EntityKeyEqualityComparer<T>.Instance.Equals(entity, this.dbContext.Set<T>().Local[i]))
					{
						localEntity = this.dbContext.Set<T>().Local[i];
					}
				}
			}

			if (localEntity == null) // 本地实体未加载，附加
			{
				var entry = this.dbContext.Entry(entity);
				if (entry.State == System.Data.EntityState.Detached)
				{
					entry.State = System.Data.EntityState.Unchanged; // 或 DbSet.Attach(entity);

				}
			}
			else
			{
				if (localEntity != entity) // 使用反射更新实体属性
				{
					entity.MemberwiseCopy(localEntity);
				}
				else // 其它情况不做处理，正常 Update
				{
				}
			}
			this.dbContext.Set<T>().Remove(entity);
			this.dbContext.SaveChanges();
		}

		public void Update<T>(T entity) where T : class
		{
			// todo: 这里的处理方式有问题，在同一个 dbContext 内缓存对象较多时，会造成性能下降，因此需要优化
			// 先从 local 中查找 entity 对应的实体是否已加载
			T localEntity = null;
			if (this.dbContext.Set<T>().Local.Count > 0)
			{
				for (int i = 0; i < this.dbContext.Set<T>().Local.Count; i++)
				{
					if (EntityKeyEqualityComparer<T>.Instance.Equals(entity, this.dbContext.Set<T>().Local[i]))
					{
						localEntity = this.dbContext.Set<T>().Local[i];
						break;
					}
				}
			}

			if (localEntity == null) // 本地实体未加载，附加
			{
				var entry = this.dbContext.Entry(entity);
				if (entry.State == System.Data.EntityState.Detached)
				{
					entry.State = System.Data.EntityState.Modified; // 或 DbSet.Attach(entity);
				}
			}
			else
			{
				if (localEntity != entity) // 使用反射更新实体属性
				{
					entity.MemberwiseCopy(localEntity);
				}
				else // 其它情况不做处理，正常 Update
				{
				}
			}
			this.dbContext.SaveChanges();
		}

		#region

		#region AddOrUpdate
		/// <summary>
		/// 添加或更新实体，该方法当指定的实体具有健值时对数据库执行一次查询以判断目标数据是否确实存在，如果存在，则执行更新操作，其它情况下执行添加操作。
		/// </summary>
		/// <typeparam name="T">实体的类型。</typeparam>
		/// <param name="entity">要添加或更新的实体。</param>
		public void AddOrUpdate<T>(T entity) where T : class
		{
			if (entity == null)
			{
				throw new ArgumentNullException("entity");
			}
			EntityMapping mapping = EntityMapping.GetEntityMapping(typeof(T));

			object[] keyValues = new object[mapping.Keys.Length];

			bool isUpdate = true;

			for (int i = 0; i < mapping.Keys.Length; i++)
			{
				keyValues[i] = EntityColumnMapping.GetKeyValue(mapping.Keys[i], entity);
				if (keyValues[i] == null)
				{
					isUpdate = false;
					break;
				}
			}

			if(isUpdate)
			{
				isUpdate = this.ExecuteScalar<int>(String.Format(mapping.selectCountByKeys, this.GetMappingToTable<T>()), EntityMapping.BuildParameters(mapping.Keys, keyValues)) > 0;
			}

			if (!isUpdate)
			{
				this.Add<T>(entity);
			}
			else
			{
				this.Update<T>(entity);
			}
		}

		private class EntityColumnMapping
		{
			public string Member;
			public string Column;

			public bool IsKey;
			public bool IsDatabaseGenerate;

			public bool IsRequired;
			public string SqlType;
			public Type ColumnType;


			public int StringLength;

			private MemberInfo memberInfo;

			public EntityColumnMapping(MemberInfo memberInfo)
			{
				this.memberInfo = memberInfo;
			}

			public static object GetValue(EntityColumnMapping colMapping, object entity)
			{
				switch (colMapping.memberInfo.MemberType)
				{
					case MemberTypes.Property:
						return ((PropertyInfo)colMapping.memberInfo).GetValue(entity, null);
					case MemberTypes.Field:
						return ((FieldInfo)colMapping.memberInfo).GetValue(entity);
					default:
						return null;
				}
			}

			public static object GetKeyValue(EntityColumnMapping colMapping, object entity)
			{
				object value = GetValue(colMapping, entity);

				if (value != null)
				{
					if (colMapping.IsKey)
					{
						if (colMapping.ColumnType == TypeHelper.String) // 注意： string 不是基元类型
						{
							if (((string)value).Length==0)
							{
								return null;
							}
						}
						else if (colMapping.ColumnType.IsPrimitive)
						{
							#region 基元类型
							// Int、Bool、Decimal 四个最常用的两个基元类放在最前面比较
							if (colMapping.ColumnType == TypeHelper.Int32)
							{
								if (((int)value) == default(int))
								{
									return null;
								}
							}
							//else if (colMapping.ColumnType == TypeHelper.Boolean)
							//{
							//}
							//else if (colMapping.ColumnType == TypeHelper.Char)
							//{
							//    if (((char)value) == '\0')
							//    {
							//        return null;
							//    }
							//}
							else
							{
								if (colMapping.ColumnType == TypeHelper.Int16)
								{
									if (((short)value) == default(short))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.Int64)
								{
									if (((long)value) <= 0)
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.SByte)
								{
									if (((sbyte)value) == default(sbyte))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.Single)
								{
									if (((float)value) == default(float))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.Double)
								{
									if (((double)value) == default(double))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.Byte)
								{
									if (((byte)value) == default(byte))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.UInt16)
								{
									if (((ushort)value) == default(ushort))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.UInt32)
								{
									if (((uint)value) == default(uint))
									{
										return null;
									}
								}
								else if (colMapping.ColumnType == TypeHelper.UInt64)
								{
									if (((ulong)value) == default(ulong))
									{
										return null;
									}
								}
							}
							#endregion
						}
						else if (colMapping.ColumnType == TypeHelper.DateTime)
						{
							if ((DateTime)value == default(DateTime))
							{
								return null;
							}
						}
						else if (colMapping.ColumnType == TypeHelper.Decimal) // 注意： decimal 不是基元类型
						{
							if (((decimal)value) == default(decimal))
							{
								return null;
							}
						}
						else if (colMapping.ColumnType == TypeHelper.TimeSpan)
						{
							if ((TimeSpan)value == default(TimeSpan))
							{
								return null;
							}
						}
						else if (colMapping.ColumnType == TypeHelper.ByteArray) // 字节数组单独处理为 {…}
						{
						}
						else if (colMapping.ColumnType.IsArray)
						{
						}
						else if (TypeHelper.IDictionary.IsAssignableFrom(colMapping.ColumnType))
						{
						}
						else if (TypeHelper.IEnumerable.IsAssignableFrom(colMapping.ColumnType))
						{
						}
						else if (colMapping.ColumnType.IsEnum) // 注意： 枚举不是基元类型
						{
						}
						else
						{
						}
					}
				}

				return value;
			}
		}

		/// <summary>
		/// 上下文无关的实体映射对象
		/// </summary>
		private class EntityMapping
		{
			private static Dictionary<Type, EntityMapping> mappings = new Dictionary<Type, EntityMapping>();

			public static EntityMapping GetEntityMapping(Type entityType)
			{
				if (entityType == null)
				{
					throw new ArgumentNullException("entityType");
				}

				EntityMapping mapping = null;
				lock (mappings)
				{
					if (mappings.ContainsKey(entityType))
					{
						mapping = mappings[entityType];
					}
					else
					{
						mapping = new EntityMapping(entityType);

						mappings.Add(entityType, mapping);
					}
				}
				return mapping;
			}

			private static object syncRoot = new object();

			private Type entityType;

			private EntityColumnMapping timestampColumn;
			private EntityColumnMapping[] keys;
			private EntityColumnMapping[] columns;

			public EntityColumnMapping[] Keys
			{
				get
				{
					return this.keys;
				}
			}

			public EntityColumnMapping[] Columns
			{
				get
				{
					return this.columns;
				}
			}

			public EntityColumnMapping TimestampColumn
			{
				get
				{
					return this.timestampColumn;
				}
			}

			private EntityMapping(Type entityType)
			{
				this.entityType = entityType;

				List<EntityColumnMapping> columnList = new List<EntityColumnMapping>();
				List<EntityColumnMapping> keyList = new List<EntityColumnMapping>();
				EntityColumnMapping tsColumn = null;

				MemberInfo[] members = this.entityType.GetMembers();
				for (int i = 0; i < members.Length; i++)
				{
					switch (members[i].MemberType)
					{
						case MemberTypes.Property:
							if (((PropertyInfo)members[i]).CanWrite)
							{
								goto case MemberTypes.Field;
							}
							break;
						case MemberTypes.Field:
							if (Attribute.GetCustomAttribute(members[i], typeof(NotMappedAttribute), true) == null)
							{
								EntityColumnMapping colMapping = new EntityColumnMapping(members[i]);
								colMapping.Member = members[i].Name;
								colMapping.Column = members[i].Name;
								colMapping.IsKey = Attribute.GetCustomAttribute(members[i], typeof(KeyAttribute), true) != null;
								colMapping.IsDatabaseGenerate = Attribute.GetCustomAttribute(members[i], typeof(DatabaseGeneratedAttribute), true) != null;

								if (colMapping.IsKey)
								{
									colMapping.IsRequired = true;
								}
								else
								{
									colMapping.IsRequired = Attribute.GetCustomAttribute(members[i], typeof(RequiredAttribute), true) != null;
								}

								colMapping.ColumnType = members[i].MemberType == MemberTypes.Property ? ((PropertyInfo)members[i]).PropertyType : ((FieldInfo)members[i]).FieldType;
								if (colMapping.ColumnType == typeof(string))
								{
									StringLengthAttribute stringLengthAttribute = ((StringLengthAttribute)Attribute.GetCustomAttribute(members[i], typeof(StringLengthAttribute), true));
									if (stringLengthAttribute != null)
									{
										colMapping.StringLength = stringLengthAttribute.MaximumLength;
									}
								}

								DataTypeAttribute dataTypeAttribute = (DataTypeAttribute)Attribute.GetCustomAttribute(members[i], typeof(DataTypeAttribute), true);
								if (dataTypeAttribute != null)
								{
									colMapping.SqlType = dataTypeAttribute.CustomDataType;
								}

								if (colMapping.ColumnType == typeof(byte[]))
								{
									if (Attribute.GetCustomAttribute(members[i], typeof(TimestampAttribute), true) != null)
									{
										tsColumn = colMapping;
									}
								}

								columnList.Add(colMapping);

								if (colMapping.IsKey)
								{
									keyList.Add(colMapping);
								}
							}
							break;
						default:
							break;
					}
				}

				this.keys = keyList.ToArray();
				this.columns = columnList.ToArray();

				this.timestampColumn = tsColumn;

				StringBuilder sb = new StringBuilder();
				sb.Append("select count(1) from {0} where ");
				BuildWhereClauseByColumns(sb, this.keys);

				this.selectCountByKeys = sb.ToString();
			}

			private static void BuildWhereClauseByColumns(StringBuilder sb, EntityColumnMapping[] cols)
			{
				for (int i = 0; i < cols.Length; i++)
				{
					if (i > 0)
					{
						sb.Append(" and ");
					}
					sb.Append(cols[0].Column).Append("=@").Append(cols[i].Column);
				}
			}

			public static Dictionary<string, object> BuildParameters(EntityColumnMapping[] cols, object[] values)
			{
				if (cols.Length != values.Length)
				{
					throw new ArgumentException();
				}

				if(cols.Length == 0)
				{
					return Empty<string, object>.Dictionary;
				}

				Dictionary<string, object> dict = new Dictionary<string, object>(cols.Length);
				for (int i = 0; i < cols.Length; i++)
				{
					dict["@" + cols[i].Column] = values[i];
				}

				return dict;
			}

			internal string selectCountByKeys;
		}
		#endregion

		#endregion

		#region FindXXX
		/// <summary>
		/// 使用指定的键值查找指定类型参数的实体。
		/// </summary>
		/// <typeparam name="T">要查询的实体的类型。</typeparam>
		/// <param name="keyValues">用来执行查找的键值。</param>
		/// <returns>如果找到，返回实体对象，否则返回 null。</returns>
		/// <remarks>
		/// 如果键是由多个字段组合而成的，此处输入组合主键的次序需要按照我们定义改实体类时声明主键的次序。
		/// </remarks>
		public T FindByPrimaryKey<T>(params object[] keyValues) where T : class
		{
			return this.dbContext.Set<T>().Find(keyValues);
		}

		/// <summary>
		/// 根据指定的属性和值查找符合条件的实体。
		/// </summary>
		/// <typeparam name="T">要查询的实体的类型。</typeparam>
		/// <param name="property">用来对实体进行过滤的属性。</param>
		/// <param name="value">用来对实体进行过滤的值。</param>
		/// <returns>所有符合条件的实体组成的集合。</returns>
		public List<T> FindByProperty<T>(string property, object value) where T : class
		{
			return this.dbContext.Set<T>().Where(property + "==@0", value).ToList();
		}

		/// <summary>
		/// 查找与“属性-值”键值对集合中的全部“属性-值”都匹配的的实体。
		/// </summary>
		/// <typeparam name="T">要查询的实体的类型。</typeparam>
		/// <param name="propertyValues">用来对实体进行过滤的属性、值键值对组成的集合。</param>
		/// <returns>所有符合条件的实体组成的集合。</returns>
		public List<T> FindByAll<T>(List<KeyValuePair<string, object>> propertyValues) where T : class
		{
			object[] values = new object[propertyValues.Count];
			StringBuilder sbKey = new StringBuilder();
			for (int i = 0; i < propertyValues.Count; i++)
			{
				if (i > 0) sbKey.Append(" and ");
				sbKey.Append(propertyValues[i].Key).Append("==@").Append(i);
				values[i] = propertyValues[i].Value;
			}
			return this.dbContext.Set<T>().Where<T>(sbKey.ToString(), values).ToList();
		}

		/// <summary>
		/// 查找与“属性-值”键值对集合中的任一“属性-值”匹配的的实体。
		/// </summary>
		/// <typeparam name="T">要查询的实体的类型。</typeparam>
		/// <param name="propertyValues">用来对实体进行过滤的属性、值键值对组成的集合。</param>
		/// <returns>所有符合条件的实体组成的集合。</returns>
		public List<T> FindByAny<T>(List<KeyValuePair<string, object>> propertyValues) where T : class
		{
			object[] values = new object[propertyValues.Count];
			StringBuilder sbKey = new StringBuilder();
			for (int i = 0; i < propertyValues.Count; i++)
			{
				if (i > 0) sbKey.Append(" or ");
				sbKey.Append(propertyValues[i].Key).Append("==@").Append(i);
				values[i] = propertyValues[i].Value;
			}
			return this.dbContext.Set<T>().Where<T>(sbKey.ToString(), values).ToList();
		}
		#endregion

		#region ExecuteList-非分页
		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的对象的集合。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>满足条件的对象的集合。</returns>
		public List<T> ExecuteList<T>(string sql, params object[] parameters)
		{
			return this.database.SqlQuery<T>(sql, parameters).ToList();
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的对象的集合。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		public List<T> ExecuteList<T>(string sql, Dictionary<string, object> parameters)
		{
			return this.database.SqlQuery<T>(sql, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters)).ToList();
		}
		#endregion

		#region ExecuteList-分页
		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		public List<T> ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, params object[] parameters)
		{
			return this.ExecuteListInternal<T>(null, fields, condition, orderBy, startIndex, count, parameters);
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		public List<T> ExecuteList<T>(string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters)
		{
			return this.ExecuteListInternal<T>(null, fields, condition, orderBy, startIndex, count, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行查询，在满足条件限制的结果集中从指定的索引位置选取指定数量的数据。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="tableName">要查询表名。</param>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="startIndex">本次要取记录从 1 开始的索引。</param>
		/// <param name="count">本次要取记录的条数。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		/// <example>
		/// entityContext.ExecuteList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		public List<T> ExecuteList<T>(string tableName, string fields, string condition, string orderBy, int startIndex, int count, Dictionary<string, object> parameters)
		{
			return this.ExecuteListInternal<T>(tableName, fields, condition, orderBy, startIndex, count, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
		}
		private List<T> ExecuteListInternal<T>(string tableName, string fields, string condition, string orderBy, int startIndex, int count, params object[] parameters)
		{
			if (String.IsNullOrEmpty(tableName))
			{
				tableName = this.GetMappingToTable(typeof(T));
			}
			if (String.IsNullOrWhiteSpace(fields))
			{
				fields = "*";
			}
			if (String.IsNullOrWhiteSpace(orderBy))
			{
				throw new ArgumentNullException("orderBy");
			}
			if (startIndex < 1)
			{
				startIndex = 1;
				// throw new ArgumentException("起始记录索引不能小于1", "startIndex");
			}
			if (count < 1)
			{
				throw new ArgumentException("所取数据条数不能小于1", "count");
			}
			if (count > 1000)
			{
				throw new ArgumentException("不能一次取超过 1000 条的数据", "count");
			}

			string query;
			// 当排序列有相同值时，使用Top方法和RowNumber方法分页的排序结果是不一样的，故取消Top排序
			//if (startIndex <= 1)
			//{
			//    query = string.Format("Select Top {0} {1} From [{2}]", count, fields, tableName);
			//    if (!String.IsNullOrWhiteSpace(condition))
			//    {
			//        query += " where " + condition;
			//    }
			//    query += " order by " + orderBy;
			//}
			//else
			//{
				query = string.Format("Select * From (Select {0},ROW_NUMBER() OVER(ORDER BY {1}) AS Rownum FROM [{2}]{3}) AS D WHERE Rownum BETWEEN {4} and {5}",

					fields, orderBy, tableName, String.IsNullOrWhiteSpace(condition) ? "" : " where " + condition, startIndex, startIndex + count - 1);
			//}

			return this.database.SqlQuery<T>(query, parameters).ToList();
		}
		#endregion

		#region ExecutePagedList-旧式分页
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <param name="condition"></param>
		/// <param name="orderBy"></param>
		/// <param name="pageIndex"></param>
		/// <param name="pageSize"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		/// <example>
		/// entityContext.ExecutePagedList&lt;Order&gt;("OrderID,Title,CustomerName", "CustomerName=@p0", "CustomerName desc", 1, 2, "entityContext");
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		public List<T> ExecutePagedList<T>(string fields, string condition, string orderBy, int pageIndex, int pageSize, params object[] parameters)
		{
			return this.ExecutePagedListInternal<T>(null, fields, condition, orderBy, pageIndex, pageSize, parameters);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fields"></param>
		/// <param name="condition"></param>
		/// <param name="orderBy"></param>
		/// <param name="pageIndex"></param>
		/// <param name="pageSize"></param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns></returns>
		public List<T> ExecutePagedList<T>(string fields, string condition, string orderBy, int pageIndex, int pageSize, Dictionary<string, object> parameters)
		{
			return this.ExecutePagedListInternal<T>(null, fields, condition, orderBy, pageIndex, pageSize, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中执行分页查询，该接口仅用于提供对现有 SQLHelper 组件的兼容性。
		/// </summary>
		/// <typeparam name="T">回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="tableName">要查询表名。</param>
		/// <param name="fields">要查询的字段，以“，”分割。</param>
		/// <param name="condition">要查询的条件。</param>
		/// <param name="orderBy">排序规则。</param>
		/// <param name="pageIndex">页码。</param>
		/// <param name="pageSize">页大小。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的对象的集合。</returns>
		public List<T> ExecutePagedList<T>(string tableName, string fields, string condition, string orderBy, int pageIndex, int pageSize, Dictionary<string, object> parameters)
		{
			return this.ExecutePagedListInternal<T>(tableName, fields, condition, orderBy, pageIndex, pageSize, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
		}

		private List<T> ExecutePagedListInternal<T>(string tableName, string fields, string condition, string orderBy, int pageIndex, int pageSize, params object[] parameters)
		{
			if(String.IsNullOrEmpty(tableName))
			{
				tableName = this.GetMappingToTable(typeof(T));
			}
			if (String.IsNullOrWhiteSpace(fields))
			{
				fields = "*";
			}
			if (String.IsNullOrWhiteSpace(orderBy))
			{
				throw new ArgumentNullException("orderBy");
			}
			if (pageIndex < 1)
			{
				throw new ArgumentException("页码不能小于1", "pageIndex");
			}
			if (pageSize < 1)
			{
				throw new ArgumentException("每页不能小于1条数据", "pageSize");
			}
			if (pageSize > 1000)
			{
				throw new ArgumentException("每页不能大于1000条数据", "pageSize");
			}

			string query;
			// 当排序列有相同值时，使用Top方法和RowNumber方法分页的排序结果是不一样的，故取消Top排序
			//if (pageIndex <= 1)
			//{
			//    query = string.Format("Select Top {0} {1} From [{2}]", pageSize, fields, tableName);
			//    if (!String.IsNullOrWhiteSpace(condition))
			//    {
			//        query += " where " + condition;
			//    }
			//    query += " order by " + orderBy;
			//}
			//else
			//{
				query = string.Format("Select * From (Select {0},ROW_NUMBER() OVER(ORDER BY {1}) AS Rownum FROM [{2}]{3}) AS D WHERE Rownum BETWEEN {4} and {5}",

					fields, orderBy, tableName, String.IsNullOrWhiteSpace(condition) ? "" : " where " + condition, (pageIndex - 1) * pageSize + 1, pageIndex * pageSize);
			//}

			return this.database.SqlQuery<T>(query, parameters).ToList();
		}
		#endregion

		/// <summary>
		/// 为指定的类型参数获取可进行 Linq 查询的数据源。
		/// </summary>
		/// <typeparam name="T">要查询的实体的类型。</typeparam>
		/// <returns>可进行 Linq 查询的数据源。</returns>
		public IQueryable<T> GetQueryableDataSource<T>() where T : class
		{
			return this.dbContext.Set<T>();
		}

		#region ExecuteXXX
		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，
		/// </summary>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>语句执行影响的行数。</returns>
		/// <remarks>
		/// 可以通过此方法执行任意不需要返回数据的 SQL 语句（如 delete、update 等）。
		/// </remarks>
		/// <example>
		/// entityContext.ExecuteNonQuery("UPDATE Person SET Name = @p0 WHERE PersonID = @p1", "Mike", 100);
		/// 注意：在直接写查询参数的情况下，查询参数必须命名为 @p0 的形式(必须从 0 开始)
		/// </example>
		public int ExecuteNonQuery(string sql, params object[] parameters)
		{
			try
			{
				return this.database.ExecuteSqlCommand(sql, parameters);
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，
		/// </summary>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 <paramref name="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>语句执行影响的行数。</returns>
		/// <remarks>
		/// 可以通过此方法执行任意不需要返回数据的 SQL 语句（如 delete、update 等）。
		/// </remarks>
		public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters)
		{
			try
			{
				return this.database.ExecuteSqlCommand(sql, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}

		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，查询结果以给定的泛型类型返回。
		/// </summary>
		/// <typeparam name="T">要返回的数据的类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>用于执行查询的枚举器。</returns>
		public IEnumerable<T> ExecuteQuery<T>(string sql, params object[] parameters)
		{
			try
			{
				return this.database.SqlQuery<T>(sql, parameters);
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}

		/// <summary>
		/// 在数据库中直接执行指定的 SQL 语句，查询结果以给定的泛型类型返回。
		/// </summary>
		/// <typeparam name="T">要返回的数据的类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>用于执行查询的枚举器。</returns>
		public IEnumerable<T> ExecuteQuery<T>(string sql, Dictionary<string, object> parameters)
		{
			try
			{
				return this.database.SqlQuery<T>(sql, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters));
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的第一条数据对应的对象实例。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">用于参数化 SQL 语句的命令参数。</param>
		/// <returns>满足条件的第一条数据对应的对象实例。</returns>
		public T ExecuteScalar<T>(string sql, params object[] parameters)
		{
			try
			{
				// 下面两种写法都没问题
				return this.database.SqlQuery<T>(sql, parameters).FirstOrDefault();
				// return this.database.SqlQuery<T>(sql, parameters).DefaultIfEmpty<T>(default(T)).First();
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}

		/// <summary>
		/// 在当前实体上下文相关的数据库中直接执行指定的 SQL 语句，返回满足条件的第一条数据对应的对象实例。
		/// </summary>
		/// <typeparam name="T">返回数据对应的实体的类型，可以是int、string 等单值对象，也可以是任意复杂对象或者匿名类型。</typeparam>
		/// <param name="sql">要执行的 SQL 语句，该语句可以是参数化的。</param>
		/// <param name="parameters">与 <see cref="sql"/> 语句匹配的由参数名称和参数值构成的键值对组成的参数集合。</param>
		/// <returns>满足条件的第一条数据对应的对象实例。</returns>
		public T ExecuteScalar<T>(string sql, Dictionary<string, object> parameters)
		{
			try
			{
				// 下面两种写法都没问题
				return this.database.SqlQuery<T>(sql, ConvertDictionaryToParameterArray(this.businessContext.dbProviderFactory, false, parameters)).FirstOrDefault();
				// return this.database.SqlQuery<T>(sql, ConvertDictionaryToParameterArray(parameters)).DefaultIfEmpty<T>(default(T)).First();
			}
			catch (System.Data.SqlClient.SqlException sqlErr)
			{
				if (sqlErr.Number == 3960)
				{
					throw new System.Data.Entity.Infrastructure.DbUpdateConcurrencyException(sqlErr.Message);
				}

				throw;
			}
		}
		#endregion


		#region 按实体键值对两个实体进行比较的比较器
		private class EntityKeyEqualityComparer<T> : EqualityComparer<T>
		{
			private static EntityKeyEqualityComparer<T> instance = new EntityKeyEqualityComparer<T>();
			public static EntityKeyEqualityComparer<T> Instance
			{
				get
				{
					return instance;
				}
			}

			private static Dictionary<Type, List<MemberInfo>> entityKeys = new Dictionary<Type, List<MemberInfo>>();

			private List<MemberInfo> GetEntityKeys(Type entityType)
			{
				List<MemberInfo> list = null;
				if (!entityKeys.ContainsKey(entityType))
				{
					MemberInfo[] members = entityType.GetMembers();
					for (int i = 0; i < members.Length; i++)
					{
						object[] attributes = members[i].GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), true);
						if (attributes != null && attributes.Length > 0)
						{
							if (list == null)
							{
								list = new List<MemberInfo>(2);
								list.Add(members[i]);
							}
						}
					}

					lock (entityKeys)
					{
						if (!entityKeys.ContainsKey(entityType))
						{
							entityKeys.Add(entityType, list);
						}
					}
				}
				else
				{
					list = entityKeys[entityType];
				}
				return list;
			}
			private object GetPropertyOrFieldMemberValue(MemberInfo member, object obj)
			{
				switch (member.MemberType)
				{
					case MemberTypes.Property:
						return ((PropertyInfo)member).GetValue(obj, null);
					case MemberTypes.Field:
						return ((FieldInfo)member).GetValue(obj);
					default:
						return null;
				}
			}

			public override bool Equals(T x, T y)
			{
				if (x != null)
				{
					if (y != null)
					{
						List<MemberInfo> keys = GetEntityKeys(typeof(T));

						if (keys != null && keys.Count > 0)
						{
							for (int i = 0; i < keys.Count; i++)
							{
								object xValue = GetPropertyOrFieldMemberValue(keys[i], x);
								object yValue = GetPropertyOrFieldMemberValue(keys[i], y);
								if (xValue == null && yValue == null)
								{
									continue;
								}
								if (xValue != null && xValue.Equals(yValue))
								{
									continue;
								}
								return false;
							}
							return true;
						}
						else
						{
							return x.Equals(y);
						}
					}
					return ((y != null) && x.Equals(y));
				}
				if (y != null)
				{
					return false;
				}
				return true;

			}

			public override int GetHashCode(T obj)
			{
				if (obj == null)
				{
					return 0;
				}
				List<MemberInfo> keys = GetEntityKeys(obj.GetType());
				if (keys.Count > 0)
				{
					int hashCode = keys[0].GetHashCode();
					for (int i = 1; i < keys.Count; i++)
					{
						hashCode = hashCode ^ keys[i].GetHashCode();
					}
					return hashCode;
				}
				return obj.GetHashCode();
			}
		}
		#endregion

		private static System.Data.Common.DbParameter[] emptyParametersArray = new System.Data.Common.DbParameter[]{};

		internal static System.Data.Common.DbParameter[] ConvertDictionaryToParameterArray(System.Data.Common.DbProviderFactory dbProviderFactory, bool mappingSourceColumn, Dictionary<string, object> parameters)
		{
			if (dbProviderFactory == null)
			{
				throw new ArgumentNullException("dbProviderFactory");
			}
			if (parameters == null || parameters.Count==0)
			{
				return emptyParametersArray;
			}

			System.Data.Common.DbParameter[] parametersArray = new System.Data.Common.DbParameter[parameters.Count];
	
			int i = 0;
			foreach(KeyValuePair<string, object> kvp in parameters)
			{
				parametersArray[i] = dbProviderFactory.CreateParameter();
				if (kvp.Key[0] == '@')
				{
					parametersArray[i].ParameterName = kvp.Key;
					if (mappingSourceColumn)
					{
						parametersArray[i].SourceColumn = kvp.Key.Substring(1);
					}
				}
				else
				{
					parametersArray[i].ParameterName = "@" + kvp.Key;
					if (mappingSourceColumn)
					{
						parametersArray[i].SourceColumn = kvp.Key;
					}
				}
				parametersArray[i].Value = kvp.Value == null ? DBNull.Value : kvp.Value;

				if (kvp.Value is string)
				{
					parametersArray[i].Size = Math.Max(4000, ((String)kvp.Value).Length);
				}
				i++;
			}

			return parametersArray;
		}

		internal static System.Data.Common.DbParameter[] ConvertStringArrayToParameterArray(System.Data.Common.DbProviderFactory dbProviderFactory, bool mappingSourceColumn, params string[] parameters)
		{
			if (dbProviderFactory == null)
			{
				throw new ArgumentNullException("dbProviderFactory");
			}
			if (parameters == null || parameters.Length == 0)
			{
				return emptyParametersArray;
			}

			System.Data.Common.DbParameter[] parametersArray = new System.Data.Common.DbParameter[parameters.Length];

			for (int i = 0; i < parameters.Length; i++)
			{
				parametersArray[i] = dbProviderFactory.CreateParameter();
				if (parameters[i][0] == '@')
				{
					parametersArray[i].ParameterName = parameters[i];
					if (mappingSourceColumn)
					{
						parametersArray[i].SourceColumn = parameters[i].Substring(1);
					}
				}
				else
				{
					parametersArray[i].ParameterName = "@" + parameters[i];
					if (mappingSourceColumn)
					{
						parametersArray[i].SourceColumn = parameters[i];
					}
				}
				parametersArray[i].Value = DBNull.Value;
			}

			return parametersArray;
		}

		#region IDisposable interface
		private bool disposed = false;
		// Implement Idisposable.
		// Do not make this method virtual.
		// A derived class should not be able to override this method.
		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			// Take yourself off of the Finalization queue 
			// to prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}
		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 释放托管和非托管资源； <b>false</b> 仅释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (this.trackedAdapters != null)
					{
						for (int i = 0; i < this.trackedAdapters.Count; i++)
						{
							this.trackedAdapters[i].Dispose();
						}
						this.trackedAdapters = null;
					}
					if (this.transaction != null)
					{
						this.transaction.Dispose();
						this.transaction = null;
					}
					this.dbContext.Dispose();
				}
			}
			disposed = true;
		}

		/// <summary>
		/// 析构函数
		/// </summary>
		~DbEntityContext()
		{
			Dispose(false);
		}
		#endregion

		/// <summary>
		/// 检查当前业务相关的数据库是否存在
		/// </summary>
		/// <returns></returns>
		public bool IsDatabaseExists()
		{
			return this.businessContext.IsDatabaseExists(this.dbContext.RunMode);
		}

		/// <summary>
		/// 检查当前上下文环境中由泛型参数类型指定的实体相关的数据表是否存在。
		/// </summary>
		/// <returns></returns>
		public bool IsTableExists<T>()
		{
			return this.businessContext.IsTableExists(this.GetMappingToTable<T>(), this.dbContext.RunMode);
		}

		/// <summary>
		/// 检查当前上下文环境中指定实体类型相关的数据表是否存在。
		/// </summary>
		/// <returns></returns>
		public bool IsTableExists(Type entityType)
		{
			return this.businessContext.IsTableExists(this.GetMappingToTable(entityType), this.dbContext.RunMode);
		}

		private List<DataTableAdapter> trackedAdapters = null;
		/// <summary>
		/// 创建一个可用于操作数据库表的数据表访问适配器。
		/// </summary>
		/// <returns></returns>
		public DataTableAdapter CreateDataTableAdapter()
		{
			System.Data.EntityClient.EntityTransaction entityTransaction = this.transaction == null ? null : (System.Data.EntityClient.EntityTransaction)this.transaction;

			DataTableAdapter adapter = new DataTableAdapter(this.businessContext.dbProviderFactory, this.database.Connection, entityTransaction==null ? null : 
				(System.Data.Common.DbTransaction)typeof(System.Data.EntityClient.EntityTransaction).InvokeMember("StoreTransaction",
						BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance, null, entityTransaction, null)
				);
			if (this.trackedAdapters == null)
			{
				this.trackedAdapters = new List<DataTableAdapter>(8);
			}

			this.trackedAdapters.Add(adapter);

			return adapter;
		}
	}
}
