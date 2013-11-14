using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	/// <summary>
	/// 常用的 Primitive 类型的扩展方法
	/// </summary>
	public static class PrimitiveHelper
	{
		private static DateTime _1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		
		/// <summary>
		/// 将指定时间转换为 1970 年以来的毫秒数。
		/// </summary>
		/// <param name="value">要转换的时间。</param>
		/// <returns>1970 年以来的毫秒数。。</returns>
		public static long ToMilliSecondsFrom1970L(this DateTime value)// where T : long, double, decimal
		{
			return (value.ToUniversalTime() - _1970).Ticks / 10000;
		}

		/// <summary>
		/// 将指定的 1970 年以来的毫秒数转换为时间格式。
		/// </summary>
		/// <param name="millisecondsFrom1970">1970 年以来的毫秒数。</param>
		/// <returns>与1970 年以来的毫秒数对应的时间对象。</returns>
		public static DateTime MilliSecondsFrom1970ToDateTime(this long millisecondsFrom1970)
		{
			return _1970.AddTicks(millisecondsFrom1970 * 10000).ToLocalTime();
		}

		/// <summary>
		/// 将指定的 1970 年以来的毫秒数转换为时间格式。
		/// </summary>
		/// <param name="millisecondsFrom1970">1970 年以来的毫秒数。</param>
		/// <returns>与1970 年以来的毫秒数对应的时间对象。</returns>
		public static DateTime MilliSecondsFrom1970ToDateTime(this double millisecondsFrom1970)
		{
			return _1970.AddMilliseconds(millisecondsFrom1970).ToLocalTime();
		}

		public static byte[] ToBytes(long value, int length)
		{
			if (length <= 0)
			{
				return Empty<byte>.Array;
			}

			byte[] bytes = new byte[length];

			int realLength = Math.Min(8, length);

			for (int i = 0; i < realLength; i++)
			{
				bytes[i] = (byte)(value >> (realLength - i - 1) * 8);
			}

			return bytes;
		}

		public static byte[] ToBytes(int value, int length)
		{
			if (length <= 0)
			{
				return Empty<byte>.Array;
			}

			byte[] bytes = new byte[length];

			int realLength = Math.Min(4, length);

			for (int i = 0; i < realLength; i++)
			{
				bytes[i] = (byte)(value >> (realLength - i - 1) * 8);
			}

			return bytes;
		}

		public static long ToInt64(this byte[] value)
		{
			if (value == null ||value.Length <= 0)
			{
				return 0;
			}
			switch (value.Length)
			{
				case 1:
					return value[0];
				case 2:
					return value[0] << 0x08 | value[1];
				case 3:
					return value[0] << 0x10 | value[1] << 0x08 | value[2];
				case 4:
					return value[0] << 0x18 | value[1] << 0x10 | value[2] << 0x08 | value[3];
				case 5:
					return value[0] << 0x20 | value[1] << 0x18 | value[2] << 0x10 | value[3] << 0x08 | value[4];
				case 6:
					return value[0] << 0x28 | value[1] << 0x20 | value[2] << 0x18 | value[3] << 0x10 | value[4] << 0x08 | value[5];
				case 7:
					return value[0] << 0x30 | value[1] << 0x28 | value[2] << 0x20 | value[3] << 0x18 | value[4] << 0x10 | value[5] << 0x08 | value[6];
				default:
					return value[0] << 0x38 | value[1] << 0x30 | value[2] << 0x28 | value[3] << 0x20 | value[4] << 0x18 | value[5] << 0x10 | value[6] << 0x08 | value[7];
			}
		}

		public static int ToInt32(this byte[] value)
		{
			if (value == null || value.Length <= 0)
			{
				return 0;
			}
			switch (value.Length)
			{
				case 1:
					return value[0];
				case 2:
					return value[0] << 0x08 | value[1];
				case 3:
					return value[0] << 0x10 | value[1] << 0x08 | value[2];
				default:
					return value[0] << 0x18 | value[1] << 0x10 | value[2] << 0x08 | value[3];
			}
		}

	}
}
