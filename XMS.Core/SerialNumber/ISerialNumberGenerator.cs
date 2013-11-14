using System;
using System.Collections.Generic;

namespace XMS.Core.SerialNumber
{
	/// <summary>
	///	定义序列号生成器所共有的接口。
	/// </summary>
	public interface ISerialNumberGenerator
	{
		/// <summary>
		/// 获取序列号生成器的键。
		/// </summary>
		string GeneratorKey
		{
			get;
		}

		/// <summary>
		/// 获取序列号池的大小。
		/// </summary>
		int PoolSize
		{
			get;
		}

		/// <summary>
		/// 获取序列号。
		/// </summary>
		/// <returns>序列号。</returns>
		ISerialNumber GetSerialNumber();
	}

	/// <summary>
	/// 表示一个序列号。
	/// </summary>
	public interface ISerialNumber
	{
		/// <summary>
		/// 获取该序列号的数字值
		/// </summary>
		long Value
		{
			get;
		}

		/// <summary>
		/// 将当前序列号的值格式化为由 numberLength 参数指定长度的字符串，不足部分补'0'，然后将 format 参数指定的字符串中的格式项替换为该字符串。
		/// </summary>
		/// <param name="format">用于对当前序列号进行格式化的字符串。</param>
		/// <param name="numberLength">当前序列号的值格式化后的长度。</param>
		/// <returns>格式化后的序列号。</returns>
		/// <example>
		/// 执行 SerialNumberGeneratorManager.Instance.GetSerialNumberGenerator("20120214").Format("20120214{0}",8) 将得到 2012021400000001、2012021400000002 等。
		/// </example>
		string Format(string format, int numberLength);

		/// <summary>
		/// 根据当前序列号的值生成一个不超过 10 的 numberLength 次方的唯一随机数，然后将该随机数格式化为由 numberLength 参数指定长度的字符串，不足部分补'0'，最后将 format 参数指定的字符串中的格式项替换为该字符串。
		/// </summary>
		/// <param name="format">用于对当前序列号进行格式化的字符串。</param>
		/// <param name="numberLength">当前序列号的值格式化后的长度。</param>
		/// <returns>格式化后的具有随机数的序列号。</returns>
		/// <example>
		/// 执行 SerialNumberGeneratorManager.Instance.GetSerialNumberGenerator("20120214").FormatWithRandom("20120214{0}",8) 将得到 2012021434657823、2012021476432345 等。
		/// </example>
		string FormatWithRandom(string format, int numberLength);
	}

	/// <summary>
	/// ISerialNumber 接口的默认实现。
	/// </summary>
	public class DefaultSerialNumber : ISerialNumber
	{
		private DateTime createTime;
		private long value;
		/// <summary>
		/// 获取该序列号的数字值
		/// </summary>
		public long Value
		{
			get
			{
				return this.value;
			}
		}

		/// <summary>
		/// 初始化 DefaultSerialNumber 的新实例。
		/// </summary>
		/// <param name="value"></param>
		public DefaultSerialNumber(long value)
		{
			this.value = value;
			this.createTime = DateTime.Now;
		}

		/// <summary>
		/// 将当前序列号的值格式化为由 numberLength 参数指定长度的字符串，不足部分补'0'，然后将 format 参数指定的字符串中的格式项替换为该字符串。
		/// </summary>
		/// <param name="format">用于对当前序列号进行格式化的字符串。</param>
		/// <param name="numberLength">当前序列号的值格式化后的长度。</param>
		/// <returns>格式化后的序列号。</returns>
		/// <example>
		/// 执行 SerialNumberGeneratorManager.Instance.GetSerialNumberGenerator("20120214").Format("20120214{0}",8) 将得到 2012021400000001、2012021400000002 等。
		/// </example>
		public string Format(string format, int numberLength)
		{
			if (String.IsNullOrEmpty(format))
			{
				return this.value.ToString(new String('0', numberLength));
			}
			return String.Format(format, this.value.ToString(new String('0', numberLength)));
		}

		/// <summary>
		/// 根据当前序列号的值生成一个不超过 10 的 numberLength 次方的唯一随机数，然后将该随机数格式化为由 numberLength 参数指定长度的字符串，不足部分补'0'，最后将 format 参数指定的字符串中的格式项替换为该字符串。
		/// </summary>
		/// <param name="format">用于对当前序列号进行格式化的字符串。</param>
		/// <param name="numberLength">当前序列号的值格式化后的长度。</param>
		/// <returns>格式化后的具有随机数的序列号。</returns>
		/// <example>
		/// 执行 SerialNumberGeneratorManager.Instance.GetSerialNumberGenerator("20120214").FormatWithRandom("20120214{0}",8) 将得到 2012021434657823、2012021476432345 等。
		/// </example>
		/// <remarks>
		/// 该方法在当前序列号的值和当前序列号产生时的时间刻度（秒）的基础上生成随机数并进行格式化，满足序列编号固定长度、唯一不重复、先生成后使用、随机不可猜、不泄露商业机密的需求。
		/// </remarks>
		public string FormatWithRandom(string format, int numberLength)
		{
			// 对时间秒数和序列号值对 10亿 求余可确保不会产生溢出
			// 计算当前时间 计时秒数 对 10亿 求余 得到的余数部分做为种子的时间参量
			int seconds = (int)(((long)(this.createTime - DateTime.MinValue).TotalSeconds) % 1000000000);

			// 用 当前时间计时秒数 + 序列号值对 10 亿 求余作为随机数种子
			// 对同一个键值来说，该种子是永远唯一的
			int seed = seconds + (int)(this.value % 1000000000);

			// 利用 Random 类的不同随机数种子产生的随机数必然不同的特性，生成由 numberLength 限定长度的随机数
			// 经测试 randomNumberLength 为 8 即最大值为 1亿 时，2000 万次调用的情况下不会产生重复的编号
			//		备注：new Random((Guid.NewGuid()).GetHashCode()).Next(100000000) 这种方法在 2000 万次调用的情况下会产生 180 万次重复
			int randomNumber = new Random(seed).Next((int)Math.Pow(10, numberLength));

			if (String.IsNullOrEmpty(format))
			{
				return randomNumber.ToString(new String('0', numberLength));
			}

			return String.Format(format, randomNumber.ToString(new String('0', numberLength)));
		}
	}
}