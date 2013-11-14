using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Reflection;
using System.Data;

using XMS.Core;
using XMS.Core.Entity;

namespace BusinessContextTest
{
	public class Order
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int OrderID { get; set; }

		[Required]
		[StringLength(32, MinimumLength = 1)]
		public string Title { get; set; }

		[Required]
		[StringLength(64, MinimumLength = 1)]
		public string CustomerName { get; set; }

		[Timestamp]
		public byte[] TS
		{
			get;
			set;
		}
	}

	[NotMapped]
	public class OrderEx : Order
	{
		[NotMapped]
		public string Summery
		{
			get
			{
				return this.Title;
			}
			set
			{
				this.Title = value;
			}
		}
	}

	public class GroupMessage
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ID
		{
			get;
			set;
		}

		[Required]
		[DataType("nvarchar")]
		[StringLength(100, MinimumLength = 1)]
		public string Title
		{
			get;
			set;
		}

		[Required]
		[DataType("ntext")]
		public string Content
		{
			get;
			set;
		}

		[Required]
		public int From
		{
			get;
			set;
		}

		[Required]
		[DataType("text")]
		public string To
		{
			get;
			set;
		}

		[Required]
		[DataType(DataType.DateTime)]
		public DateTime SendTime
		{
			get;
			set;
		}

		[Required]
		[DataType(DataType.DateTime)]
		public DateTime ValidPeriodFrom
		{
			get;
			set;
		}

		[Required]
		[DataType(DataType.DateTime)]
		public DateTime ValidPeriodTo
		{
			get;
			set;
		}

		[Required]
		public bool IsDeleted
		{
			get;
			set;
		}
	}
	// 库水平分区示例
	public class OrderBusinessContext2 : DbBusinessContextBase
	{
		private int userId;
		private int cityId;

		public int UserId
		{
			get
			{
				return this.userId;
			}
		}

		public int CityId
		{
			get
			{
				return this.cityId;
			}
		}

		public OrderBusinessContext2(int userId, int cityId, string connectionStringKey)
			: base(connectionStringKey)
		{
			this.userId = userId;
			this.cityId = cityId;
			this.DatabaseName = base.DatabaseName + (userId / 100000 + 1);
		}

		private Dictionary<Type, string> modelMappings = null;

		public override Dictionary<Type, string> GetModelMappings()
		{
			if (this.modelMappings == null)
			{
				this.modelMappings = new Dictionary<Type, string>();

				this.modelMappings.Add(typeof(Order), "Order");
			}
			return this.modelMappings;
		}
	}

	// 表水平分区示例
	public class OrderBusinessContext : DbBusinessContextBase
	{
		private int userId;
		private int cityId;

		public int UserId
		{
			get
			{
				return this.userId;
			}
		}

		public int CityId
		{
			get
			{
				return this.cityId;
			}
		}

		public OrderBusinessContext(int userId, int cityId, string connectionStringKey)
			: base(connectionStringKey)
		{
			this.userId = userId;
			this.cityId = cityId;
		}

		private string tablePartitionKey = null;
		// 表水平分区示例，同一领域模型中，全部情况下键的数量决定了表分区的数量，表分区的命名必须与键相同。
		protected override string TablePartitionKey
		{
			get
			{
				if (this.tablePartitionKey == null)
				{
					this.tablePartitionKey = (this.UserId / 100000 + 1).ToString();
				}
				return this.tablePartitionKey;
			}
		}

		private Dictionary<Type, string> modelMappings = null;

		public override Dictionary<Type, string> GetModelMappings()
		{
			if (this.modelMappings == null)
			{
				this.modelMappings = new Dictionary<Type, string>();

				#region 表水平分区示例，必须与 GetEntityContextKey 配合使用
				this.modelMappings.Add(typeof(Order), this.GetPartitionTableName("Order"));
				#endregion

				this.modelMappings.Add(typeof(GroupMessage), "GroupMessages");
			}
			return this.modelMappings;
		}

		protected internal override string GetPartitionTableName(string rawTableName)
		{
			switch (rawTableName.ToLower())
			{
				case "order":
					return "Order_" + this.TablePartitionKey;
				default:
					return rawTableName;
			}
		}

		protected internal override void CreateTable(IDatabase db, string tableName, Type entityType)
		{
			if (entityType == typeof(Order))
			{
				// todo:根据模型自动创建表
				db.ExecuteNonQuery(String.Format(@"
CREATE TABLE [{0}](
	[OrderID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](32) NOT NULL,
	[CustomerName] [nvarchar](64) NOT NULL,
	[TS] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderID] ASC
)
) ON [PRIMARY]", tableName));
			}
		}

	}

	// 表水平分区示例
	public class GroupMessageBusinessContext : DbBusinessContextBase
	{
		public GroupMessageBusinessContext(string connectionStringKey)
			: base(connectionStringKey)
		{
		}

		private Dictionary<Type, string> modelMappings = null;

		public override Dictionary<Type, string> GetModelMappings()
		{
			if (this.modelMappings == null)
			{
				this.modelMappings = new Dictionary<Type, string>();

				this.modelMappings.Add(typeof(GroupMessage), "GroupMessages");
			}
			return this.modelMappings;
		}
	}

	[TestClass]
	public class BusinessContextTest
	{
		//private static string connStr = "Data Source=ZHAIXD;Initial Catalog=WQ_Orders;User ID=sa;Password=sa;";
		private static string connStr = "server=Initial Catalog=192.168.1.30;user id=sa;password=sa;database=WQ_Orders;min pool size=4;max pool size=10;packet size=1024";

		private int userId = 100;
		public int UserId
		{
			get
			{
				return userId;
			}
			
		}
		public int CityId
		{
			get
			{
				return 20000;
			}

		}

		public string CustomerA
		{
			get
			{
				return "AAA";
			}
		}
		public string CustomerB
		{
			get
			{
				return "BBB";
			}
		}

		public BusinessContextTest()
		{
		}

		// 分区测试,按用户 ID 每10万一个分区表
		[TestMethod]
		public void Partition()
		{
			// Order1
			this.userId = 100;

			this.Add();

			// order2
			this.userId = 120000;

			this.Add();

			this.Update();

			this.Delete();

			this.FindByPrimaryKey();

			this.FindByProperty();

			this.FindByAll();

			this.FindByAny();

			this.ExecuteList();

			this.ExecuteNonQuery();

			this.ExecutePagedList();

			this.ExecuteScalar();

			this.QueryableDataSource();

			// order2
			this.userId = 120000;

			this.Add();

			this.Update();

			this.Delete();

			this.FindByPrimaryKey();

			this.FindByProperty();

			this.FindByAll();

			this.FindByAny();

			this.ExecuteList();

			this.ExecuteNonQuery();

			this.ExecutePagedList();

			this.ExecuteScalar();

			this.QueryableDataSource();

			// order3
			this.userId = 320000;

			this.Add();

			this.Update();

			this.Delete();

			this.FindByPrimaryKey();

			this.FindByProperty();

			this.FindByAll();

			this.FindByAny();

			this.ExecuteList();

			this.ExecuteNonQuery();

			this.ExecutePagedList();

			this.ExecuteScalar();

			this.QueryableDataSource();

		}

		[TestMethod]
		public void AddOrUpdate()
		{
			bool exists = false;

			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);
			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				exists = entityContext.IsTableExists(typeof(Order));

				exists = entityContext.IsDatabaseExists();

				Order o = new Order();
				o.Title = CustomerA + "'s Order";
				o.CustomerName = CustomerA;

				entityContext.Add<Order>(o);

				o.CustomerName = "a";

				entityContext.AddOrUpdate<Order>(o);

				List<OrderEx> orders = entityContext.ExecuteList<OrderEx>("select * from " + entityContext.GetMappingToTable(typeof(Order)));

				Assert.IsTrue(orders.Count > 0);

				Assert.IsTrue(o.OrderID > 0);
			}

			GroupMessageBusinessContext groupBC = new GroupMessageBusinessContext(connStr);

			using (IEntityContext entityContext = groupBC.CreateEntityContext())
			{
				GroupMessage message = new GroupMessage();
				message.Title = "test";
				message.Content = "testtest";
				message.From = UserId;
				message.To = "all";
				message.SendTime = DateTime.Now;
				message.ValidPeriodFrom = DateTime.Now;
				message.ValidPeriodTo = DateTime.Now.AddYears(1);
				message.IsDeleted = false;

				entityContext.Add<GroupMessage>(message);

				Assert.IsTrue(message.ID > 0);
			}
		}

		[TestMethod]
		public void Add()
		{
			bool exists = false;

			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);
			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				exists = entityContext.IsTableExists(typeof(Order));

				exists = entityContext.IsDatabaseExists();

				Order o = new Order();
				o.Title = CustomerA + "'s Order";
				o.CustomerName = CustomerA;

				entityContext.Add<Order>(o);

				List<OrderEx> orders = entityContext.ExecuteList<OrderEx>("select * from " + entityContext.GetMappingToTable(typeof(Order)));

				Assert.IsTrue(orders.Count > 0);

				Assert.IsTrue(o.OrderID > 0);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				GroupMessage message = new GroupMessage();
				message.Title = "test";
				message.Content = "testtest";
				message.From = UserId;
				message.To = "all";
				message.SendTime = DateTime.Now;
				message.ValidPeriodFrom = DateTime.Now;
				message.ValidPeriodTo = DateTime.Now.AddYears(1);
				message.IsDeleted = false;

				entityContext.Add<GroupMessage>(message);

				Assert.IsTrue(message.ID > 0);
			}
		}

		[TestMethod]
		public void DataTabaleAdapterTest()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o1 = new Order();
			o1.Title = CustomerA + "'s Order";
			o1.CustomerName = CustomerA;

			Order o2 = new Order();
			o2.Title = CustomerA + "'s Order";
			o2.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				o1 = entityContext.FindByPrimaryKey<Order>(3);

				entityContext.BeginTransaction();
				try
				{
					//entityContext.Add<Order>(o1);

					entityContext.Add<Order>(o2);

					DataTable table = null;
					XMS.Core.Data.DataTableAdapter adapter = entityContext.CreateDataTableAdapter();

					table = new DataTable(entityContext.GetPartitionTableName("Order"));
					adapter.SetSelectCommand("select * from " + entityContext.GetPartitionTableName("Order")).Fill(table);

					for (int i = 0; i < table.Rows.Count; i++)
					{
						if (table.Rows[i]["OrderId"].ToString() != "3")
						{
							table.Rows[i]["Title"] = table.Rows[i]["Title"].ToString() + i.ToString();
						}
					}
					adapter.BuildCommands().Update(table);



					if (table != null)
					{
						XMS.Core.Data.DataTableAdapter adapter2 = entityContext.CreateDataTableAdapter();

						for (int i = 0; i < table.Rows.Count; i++)
						{
							if (table.Rows[i]["OrderId"].ToString() != "3")
							{
								table.Rows[i]["CustomerName"] = table.Rows[i]["CustomerName"].ToString() + i.ToString();
							}
							//if ((int)table.Rows[i]["OrderId"] > 10)
							//{
							//    table.Rows[i].Delete();
							//}
						}

						//adapter2.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName,A=@A where OrderId=@OrderId",
						//"Title", "CustomerName", "OrderId", "TS", "A", "I"
						//);
						//adapter2.SetDeleteCommand("delete from " + entityContext.GetPartitionTableName("Order") + " where OrderId=@OrderId", "OrderId");

						adapter2.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName,A=@A where OrderId=@OrderId",
							new System.Data.Common.DbParameter[]{
									adapter2.CreateParameter("Title", DbType.String, 100),
									adapter2.CreateParameter("@CustomerName", DbType.String, 200),
									adapter2.CreateParameter("@A", DbType.String, 1000),
									adapter2.CreateParameter("OrderId", DbType.Int32)
								});

						adapter2.SetDeleteCommand("delete from " + entityContext.GetPartitionTableName("Order") + " where OrderId=@OrderId",
							new System.Data.Common.DbParameter[]{
									adapter2.CreateParameter("OrderId", DbType.Int32)
								});

						Dictionary<string, object> parameters = new Dictionary<string, object>();
						parameters["Title"] = null;
						parameters["CustomerName"] = null;
						parameters["OrderId"] = null;
						parameters["TS"] = null;
						parameters["A"] = null;

						//adapter2.SetUpdateCommand("update " + entityContext.GetPartitionTableName("Order") + " set Title=@Title,CustomerName=@CustomerName where OrderId=@OrderId",
						//    new System.Data.Common.DbParameter[]{
						//    XMS.Core.Data.DataTableAdapter.CreateParameter("@Title", SqlDbType.NVarChar, 32),
						//    XMS.Core.Data.DataTableAdapter.CreateParameter("@CustomerName", SqlDbType.NVarChar, 64),
						//    XMS.Core.Data.DataTableAdapter.CreateParameter("@OrderId", SqlDbType.Int),
						//    XMS.Core.Data.DataTableAdapter.CreateParameter("@TS", SqlDbType.Binary)
						//    }
						//    );

						//XMS.Core.Data.DataTableAdapter.AssignParameterValues(adapter2.UpdateCommand.Parameters, table.Rows[0]);

						adapter2.Update(table);
					}

					o1.CustomerName = "test";

					entityContext.Update<Order>(o1);

					//o1.CustomerName = CustomerB;
					//entityContext.Update(o1);

					entityContext.Commit();
				}
				catch (Exception err)
				{
					entityContext.Rollback();
					throw err;
				}

				Order order = entityContext.FindByPrimaryKey<Order>(o1.OrderID);

				//Assert.IsNotNull(order);
				//Assert.AreEqual(order.CustomerName, CustomerB);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o1.OrderID);

				Assert.IsNotNull(order);
				Assert.AreEqual(order.CustomerName, CustomerB);
			}
		}

		// 事务功能测试
		[TestMethod]
		public void Transaction()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o1 = new Order();
			o1.Title = CustomerA + "'s Order";
			o1.CustomerName = CustomerA;

			Order o2 = new Order();
			o2.Title = "Test";
			o2.CustomerName = CustomerB;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				o1 = entityContext.FindByPrimaryKey<Order>(88);

				entityContext.BeginTransaction();
				try
				{
					//o1.CustomerName = "test11";

					//entityContext.Update<Order>(o1);

					entityContext.Add<Order>(o2);

					entityContext.Commit();
				}
				catch (Exception err)
				{
					entityContext.Rollback();
					throw err;
				}
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o2.OrderID);
			}
		}
	
		// 事务功能测试
		[TestMethod]
		public void Transaction2()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o2 = new Order();
			o2.Title = CustomerA + "'s Order";
			o2.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order o1 = entityContext.FindByPrimaryKey<Order>(1);

				o1.CustomerName = "test delete";

				entityContext.Update<Order>(o1);


				entityContext.BeginTransaction();
				try
				{
					entityContext.Add<Order>(o2);

					entityContext.Delete<Order>(o1);

					entityContext.Commit();
				}
				catch (Exception err)
				{
					entityContext.Rollback();
					throw err;
				}

				Assert.IsTrue(o2.OrderID > 0);
			}
		}

		// 事务功能测试
		[TestMethod]
		public void Transaction3()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o2 = new Order();
			o2.Title = CustomerA + "'s Order";
			o2.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order o1 = entityContext.FindByPrimaryKey<Order>(4);

				entityContext.Delete<Order>(o1);
				
				entityContext.BeginTransaction();
				try
				{
					o1.CustomerName = "test delete";

					entityContext.Update<Order>(o1);
					
					entityContext.Add<Order>(o2);


					entityContext.Commit();
				}
				catch (Exception err)
				{
					entityContext.Rollback();
					throw err;
				}

				Assert.IsTrue(o2.OrderID > 0);
			}
		}

		[TestMethod]
		public void Update()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.BeginTransaction();
				try
				{
					entityContext.Add<Order>(o);

					Assert.IsTrue(o.OrderID > 0);

					o.CustomerName = CustomerB;
					entityContext.Update(o);

					entityContext.Commit();
				}
				catch
				{
					entityContext.Rollback();
				}
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o.OrderID);

				Assert.IsNotNull(order);
				Assert.AreEqual(order.CustomerName, CustomerB);
			}
		}

		[TestMethod]
		public void Update2()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.Add<Order>(o);

				Assert.IsTrue(o.OrderID > 0);

				Order o2 = new Order();
				o2.CustomerName = CustomerB;
				o2.OrderID = o.OrderID;
				o2.Title = o.Title;

				entityContext.Update(o2);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o.OrderID);

				Assert.IsNotNull(order);
				Assert.AreEqual(order.CustomerName, CustomerB);
			}
		}
		[TestMethod]
		public void Delete()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.Add<Order>(o);

				Assert.IsTrue(o.OrderID > 0);

				entityContext.Delete(o);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o.OrderID);

				Assert.IsNull(order);
			}
		}

		#region Find
		[TestMethod]
		public void FindByPrimaryKey()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.Add<Order>(o);

				Assert.IsTrue(o.OrderID > 0);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Order order = entityContext.FindByPrimaryKey<Order>(o.OrderID);

				Assert.IsNotNull(order);
			}
		}


		[TestMethod]
		public void FindByProperty()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);
			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<Order> orders = entityContext.FindByProperty<Order>("CustomerName", CustomerA);

				Assert.IsTrue(orders.Count > 0);
			}
		}

		[TestMethod]
		public void FindByAll()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			List<KeyValuePair<string, object>> propertyValues = new List<KeyValuePair<string, object>>();
			propertyValues.Add(new KeyValuePair<string, object>("CustomerName", CustomerA));
			propertyValues.Add(new KeyValuePair<string, object>("Title", CustomerA + "'s Order"));

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<Order> orders = entityContext.FindByAll<Order>(propertyValues);

				Assert.IsTrue(orders.Count >= 0);
			}
		}

		[TestMethod]
		public void FindByAny()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			List<KeyValuePair<string, object>> propertyValues = new List<KeyValuePair<string, object>>();
			propertyValues.Add(new KeyValuePair<string, object>("CustomerName", CustomerA));
			propertyValues.Add(new KeyValuePair<string, object>("CustomerName", CustomerB));

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<Order> orders = entityContext.FindByAny<Order>(propertyValues);

				Assert.IsTrue(orders.Count >= 0);
			}
		}
		#endregion

		[TestMethod]
		public void ExecutePagedList()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<Order> orders = entityContext.ExecutePagedList<Order>("OrderID,Title,CustomerName", "CustomerName=@p0 or CustomerName=@p1", "CustomerName desc", 2, 5, new object[]{ CustomerA, CustomerB });

				Assert.IsTrue(orders.Count >= 0 && orders.Count <= 5);

				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["CustomerName1"] = CustomerA;
				parameters["CustomerName2"] = CustomerB;
				orders = entityContext.ExecutePagedList<Order>("OrderID,Title,CustomerName", "CustomerName=@CustomerName1 or CustomerName=@CustomerName2", "CustomerName desc", 2, 5, parameters);

				Assert.IsTrue(orders.Count >= 0 && orders.Count <= 5);
			}

		}

		[TestMethod]
		public void ExecuteList_Paged()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<Order> orders = entityContext.ExecuteList<Order>("OrderID,Title,CustomerName,TS", "CustomerName=@p0 or CustomerName=@p1", "CustomerName desc", 2, 5, new object[] { CustomerA, CustomerB });

				Assert.IsTrue(orders.Count >= 0 && orders.Count <= 5);

				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["CustomerName1"] = CustomerA;
				parameters["CustomerName2"] = CustomerB;
				orders = entityContext.ExecutePagedList<Order>("OrderID,Title,CustomerName,TS", "CustomerName=@CustomerName1 or CustomerName=@CustomerName2", "CustomerName desc", 2, 5, parameters);

				Assert.IsTrue(orders.Count >= 0 && orders.Count <= 5);
			}

		}

		[TestMethod]
		public void QueryableDataSource()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				IQueryable<Order> queryable = entityContext.GetQueryableDataSource<Order>();

				List<Order> orders1 = queryable
					.OrderByDescending(order => order.CustomerName)
					.ThenBy(order => order.OrderID)
					.Where(order => order.CustomerName == CustomerA || order.CustomerName == CustomerB )
					.Skip(10).Take(5).ToList();

				Assert.IsTrue(orders1.Count >= 0 && orders1.Count <= 5);

				List<Order> orders2 = (from order in queryable
									   where order.CustomerName == CustomerA || order.Title == CustomerB
									   orderby order.CustomerName descending, order.OrderID ascending
									   select order
								 )
								.Skip(10).Take(5).ToList();

				Assert.IsTrue(orders2.Count >= 0 && orders2.Count <= 5);
			}
		}

		[TestMethod]
		public void ExecuteNonQuery()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.Add<Order>(o);

				Assert.IsTrue(o.OrderID > 0);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.ExecuteNonQuery("delete from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where OrderID = @p0", o.OrderID);

				Order order = entityContext.FindByPrimaryKey<Order>(o.OrderID);

				Assert.IsNull(order);

				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["OrderID"] = o.OrderID;
				Assert.IsTrue(0 == entityContext.ExecuteNonQuery("delete from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where OrderID = @OrderID", parameters));
			}
		}

		[TestMethod]
		public void ExecuteScalar()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			Order o = new Order();
			o.Title = CustomerA + "'s Order";
			o.CustomerName = CustomerA;

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				entityContext.Add<Order>(o);

				Assert.IsTrue(o.OrderID > 0);
			}

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["CustomerName"] = "not exist customer";
				int orderId = entityContext.ExecuteScalar<int>("select OrderId from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where CustomerName=@CustomerName", parameters);

				Assert.IsTrue(orderId == 0);

				string customerName = entityContext.ExecuteScalar<string>("select CustomerName from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where OrderID = @p0", o.OrderID);

				Assert.AreEqual(customerName, CustomerA);

				parameters.Clear();
				parameters["OrderID"] = o.OrderID;

				Assert.AreEqual(entityContext.ExecuteScalar<string>("select CustomerName from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where OrderID = @OrderID", parameters), CustomerA);
			}
		}

		[TestMethod]
		public void ExecuteList()
		{
			OrderBusinessContext businessContext = new OrderBusinessContext(UserId, CityId, connStr);

			using (IEntityContext entityContext = businessContext.CreateEntityContext())
			{
				List<int> list = entityContext.ExecuteList<int>("select OrderID from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where CustomerName = @p0", CustomerA);

				Assert.IsTrue(list.Count >= 0);

				Dictionary<string, object> parameters = new Dictionary<string, object>();
				parameters["CustomerName"] = CustomerA;

				list = entityContext.ExecuteList<int>("select OrderID from [" + entityContext.GetMappingToTable(typeof(Order)) + "] where CustomerName = @CustomerName", parameters);

				Assert.IsTrue(list.Count >= 0);
			}
		}
	}
}
