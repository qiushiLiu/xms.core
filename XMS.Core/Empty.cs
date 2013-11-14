using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	/// <summary>
	/// 提供统一的获取指定类型空值的方法。
	/// </summary>
	public class Empty
	{
		/// <summary>
		/// 表示空字符串。
		/// </summary>
		public readonly static string String = String.Empty;

		/// <summary>
		/// 表示空 Hashtable。
		/// </summary>
		public readonly static Hashtable Hashtable = new Hashtable(0);

		/// <summary>
		/// 表示空 ArrayList。
		/// </summary>
		public readonly static ArrayList ArrayList = new ArrayList(0);

		/// <summary>
		/// 表示空 Queue。
		/// </summary>
		public readonly static Queue Queue = new Queue(0);

		/// <summary>
		/// 表示空 Stack。
		/// </summary>
		public readonly static Stack Stack = new Stack(0);

		/// <summary>
		/// 表示空 SortedList。
		/// </summary>
		public readonly static SortedList SortedList = new SortedList(0);
	}

	/// <summary>
	/// 提供统一的获取指定类型的值或空数组的方法。
	/// </summary>
	/// <typeparam name="T">空对象的类型。</typeparam>
	public class Empty<T>
	{
		/// <summary>
		/// 表示空泛型数组。
		/// </summary>
		public readonly static T[] Array = new T[] { };

		/// <summary>
		/// 表示空泛型 HashSet。
		/// </summary>
		public readonly static HashSet<T> HashSet = new HashSet<T>();

		/// <summary>
		/// 表示空泛型 List。
		/// </summary>
		public readonly static List<T> List = new List<T>(0);

		/// <summary>
		/// 表示空泛型 LinkedList。
		/// </summary>
		public readonly static LinkedList<T> LinkedList = new LinkedList<T>();

		/// <summary>
		/// 表示空泛型 Queue。
		/// </summary>
		public readonly static Queue<T> Queue = new Queue<T>(0);

		/// <summary>
		/// 表示空泛型 Stack。
		/// </summary>
		public readonly static Stack<T> Stack = new Stack<T>(0);
	}

	/// <summary>
	/// 提供统一的获取指定类型的值或空数组的方法。
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	public class Empty<TKey, TValue>
	{
		/// <summary>
		/// 表示空泛型 Dictionary。
		/// </summary>
		public readonly static Dictionary<TKey, TValue> Dictionary = new Dictionary<TKey, TValue>(0);

		/// <summary>
		/// 表示空泛型 SortedDictionary。
		/// </summary>
		public readonly static SortedDictionary<TKey, TValue> SortedDictionary = new SortedDictionary<TKey, TValue>();
	}
}
