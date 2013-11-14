using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

namespace XMS.Core.SerialNumber
{
	/*
CREATE TABLE [dbo].[SerialNumberSeed](
	[Key] [nvarchar](30) NOT NULL,
	[CurrentValue] [int] NOT NULL,
	[TS] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Key] ASC
) ON [PRIMARY]
) ON [PRIMARY]
	 */
	/// <summary>
	/// 序列号种子，用于存储指定键值序列号生成器的种子。
	/// </summary>
	public class SerialNumberSeed
	{
		/// <summary>
		/// 生成器的键。
		/// </summary>
		[Key]
		[StringLength(30)]
		[Required]
		[DataType("varchar(30)")]
		public string Key
		{
			get;
			set;
		}

		/// <summary>
		/// 当前序列号值
		/// </summary>
		[Required]
		public long CurrentValue { get; set; }

		/// <summary>
		/// 时间戳
		/// </summary>
		[Timestamp]
		public byte[] TS
		{
			get;
			set;
		}
	}
}