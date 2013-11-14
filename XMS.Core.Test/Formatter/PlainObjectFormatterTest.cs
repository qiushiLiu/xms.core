using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XMS.Core.Test.ObjectFormatter
{
	[TestClass]
	public class PlainObjectFormatterTest
	{
		[TestMethod]
		public void TestMethod1()
		{
			System.Diagnostics.Trace.WriteLine(XMS.Core.Formatter.PlainObjectFormatter.Full.Format(TestObject.Instance));

			System.Diagnostics.Trace.WriteLine(XMS.Core.Formatter.PlainObjectFormatter.Simplified.Format(TestObject.Instance));

			#region 注意，该测试输出的结果格式化后大致如下所示
//{
//    List: {
//        NULL, 1, 1.1, '2012-3-13 11:51:05', 
//        {
//            List: {}, 
//            Bytes: {…}, 
//            String: "1234567891011121314151617181920212223242526272829303132...", 
//            Char: "a", 
//            True: true, 
//            SByte: 16, 
//            Short: 10000, 
//            Int: 100000, 
//            Long: 1234567890123456, 
//            Decimal: {}, 
//            Single: 100.01, 
//            Double: 100.0001, 
//            DateTime: '2012-3-13 11:51:58', 
//            TimeSpan: '00:01:00', 
//            Array: {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, …}, 
//            MultiRankArray: {{{1, 2, 4, 5}, {3, 4, 5, 6}}, {{5, 6, 6, 7}, {7, 8, 9, 10}}, {{9, 10, 1, 2}, {11, 12, 2, 3}}}, 
//            NestedArray: {{1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, …}
//        }, 
//        {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, …},
//        {{1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, …}, 
//        {{{1, 2, 4, 5}, {3, 4, 5, 6}}, {{5, 6, 6, 7}, {7, 8, 9, 10}}, {{9, 10, 1, 2}, {11, 12, 2, 3}}},
//        {
//            a: "aa", 
//            b: 1, 
//            c: '2012-3-13 11:51:05', 
//            e: {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, …}, 
//            f: {
//                List: {}, 
//                Bytes: {…}, 
//                String: "1234567891011121314151617181920212223242526272829303132...", 
//                Char: "a", 
//                True: true, 
//                SByte: 16, 
//                Short: 10000, 
//                Int: 100000, 
//                Long: 1234567890123456, 
//                Decimal: {}, 
//                Single: 100.01, 
//                Double: 100.0001, 
//                DateTime: '2012-3-13 11:51:58', 
//                TimeSpan: '00:01:00', 
//                Array: {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, …}, 
//                MultiRankArray: {{{1, 2, 4, 5}, {3, 4, 5, 6}}, {{5, 6, 6, 7}, {7, 8, 9, 10}}, {{9, 10, 1, 2}, {11, 12, 2, 3}}}, 
//                NestedArray: {{1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, …}}, 				g: 2,
//                h: "abcdefghijklmnopqrstuvwxyz", 
//                i: 3, j: 4, k: 5, l: 6, m: 7, n: 8, o: 9, p: 1, q: 2, 
//                …
//            }, 
//            1, 1, 1, 1, 1, 1, 1, 
//        …}, 
//    Bytes: {…}, 
//    String: "1234567891011121314151617181920212223242526272829303132...", 
//    Char: "a", 
//    True: true, 
//    SByte: 16, 
//    Short: 10000, 
//    Int: 100000, 
//    Long: 1234567890123456, 
//    Decimal: {}, 
//    Single: 100.01, 
//    Double: 100.0001, 
//    DateTime: '2012-3-13 11:51:58', 
//    TimeSpan: '00:01:00', 
//    Array: {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, …}, 
//    MultiRankArray: {{{1, 2, 4, 5}, {3, 4, 5, 6}}, {{5, 6, 6, 7}, {7, 8, 9, 10}}, {{9, 10, 1, 2}, {11, 12, 2, 3}}}, 
//    NestedArray: {{1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, {1, 2, 3}, {4, 5, 6}, …}
//}
			#endregion
		}
	}

	public class TestObject
	{
		private static TestObject instance = new TestObject(false);
		public static TestObject Instance
		{
			get
			{
				return instance;
			}
		}

		private List<object> list = new List<object>();
		public List<object> List
		{
			get
			{
				return this.list;
			}
		}

		protected TestObject(bool leaf)
		{
			if (!leaf)
			{
				Dictionary<string, object> dict = new Dictionary<string,object>();
				dict.Add("a", "aa");
				dict.Add("b", 1);
				dict.Add("c", DateTime.Now);
				dict.Add("e", this.Array);
				dict.Add("f", new TestObject2());
				dict.Add("g", 2);
				dict.Add("h", "abcdefghijklmnopqrstuvwxyz");
				dict.Add("i", 3);
				dict.Add("j", 4);
				dict.Add("k", 5);
				dict.Add("l", 6);
				dict.Add("m", 7);
				dict.Add("n", 8);
				dict.Add("o", 9);
				dict.Add("p", 1);
				dict.Add("q", 2);
				dict.Add("r", 2);
				dict.Add("s", 2);
				dict.Add("t", 2);
				dict.Add("u", 3);
				dict.Add("v", 3);
				dict.Add("w", 3);
				dict.Add("x", 4);
				dict.Add("y", 4);
				dict.Add("z", 4);

				list.Add(null);
				list.Add(1);
				list.Add(1.1);
				list.Add(DateTime.Now);
				list.Add(new TestObject2());
				list.Add(this.Array);
				list.Add(this.NestedArray);
				list.Add(this.MultiRankArray);
				list.Add(dict);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
				list.Add(1);
			}
		}

		public class TestObject2 : TestObject
		{
			public TestObject2()
				: base(true)
			{
			}
		}

		public int[] Array = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
		public int[, ,] MultiRankArray = new int[3, 2, 4] { { { 1, 2, 4, 5 }, { 3, 4, 5, 6 } }, { { 5, 6, 6, 7 }, { 7, 8, 9, 10 } }, { { 9, 10, 1, 2 }, { 11, 12, 2, 3 } } };
		public int[][] NestedArray = new int[20][]{
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6},
			new int[]{1,2,3},
			new int[]{4,5,6}
		};

		public Byte[] Bytes
		{
			get
			{
				return new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 };
			}
		}

		public String String
		{
			get
			{
				return "1234567891011121314151617181920212223242526272829303132";
			}
		}

		public Char? Char
		{
			get
			{
				return 'a';
			}
		}

		public bool? True
		{
			get
			{
				return true;
			}
		}

		public sbyte? SByte
		{
			get
			{
				return 16;
			}
		}

		public short? Short
		{
			get
			{
				return 10000;
			}
		}
		public int Int
		{
			get
			{
				return 100000;
			}
		}

		public long Long
		{
			get
			{
				return 1234567890123456;
			}
		}

		public decimal Decimal
		{
			get
			{
				return 100.01m;
			}
		}
		public float Single
		{
			get
			{
				return 100.01f;
			}
		}
		public double Double
		{
			get
			{
				return 100.0001d;
			}
		}

		public DateTime DateTime
		{
			get
			{
				return DateTime.Now;
			}
		}

		public TimeSpan TimeSpan
		{
			get
			{
				return TimeSpan.FromMinutes(1);
			}
		}

	}
}
