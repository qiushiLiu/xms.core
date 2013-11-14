using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;

using XMS.Core;
using XMS.Core.Caching;
using XMS.Core.Entity;
using XMS.Core.Members;

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

/*
	// 使用方法：
	// TransferRecordNoGeneratorManager.Instance.GetSerialNumberGenerator("20120214").GetSrialNumber().FormatWithRandom("20120214{0}", 8);

	public class TransferRecordNoGeneratorManager : SerialNumberGeneratorManager
	{
		private static TransferRecordNoGeneratorManager instance = new TransferRecordNoGeneratorManager();

		/// <summary>
		/// 
		/// </summary>
		public static TransferRecordNoGeneratorManager Instance
		{
			get
			{
				return instance;
			}
		}

		/// <summary>
		/// 创建池大小为 100、初始种子为 0、步长为 1 的默认序列号生成器。
		/// </summary>
		/// <param name="generatorKey"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		protected override ISerialNumberGenerator CreateSerialNumberGenerator(string generatorKey)
		{
			return new DefaultSerialNumberGenerator(this, Container.ConfigService.GetConnectionString(Constants.ConnectionStringKey),
				generatorKey, 0, 1, 100);
		}
	}
*/
}