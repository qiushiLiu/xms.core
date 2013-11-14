using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core
{
	internal class TypeHelper
	{
		public readonly static Type Object = typeof(Object);

		public readonly static Type ByteArray = typeof(Byte[]);

		public readonly static Type String = typeof(String);

		public readonly static Type Char = typeof(Char);

		public readonly static Type Boolean = typeof(Boolean);

		#region 有符号整数
		public readonly static Type SByte = typeof(sbyte);
		public readonly static Type Int16 = typeof(short);
		public readonly static Type Int32 = typeof(int);
		public readonly static Type Int64 = typeof(long);
		#endregion

		#region 无符号整数
		public readonly static Type Byte = typeof(byte);
		public readonly static Type UInt16 = typeof(ushort);
		public readonly static Type UInt32 = typeof(uint);
		public readonly static Type UInt64 = typeof(ulong);
		#endregion

		public readonly static Type Decimal = typeof(decimal);
		public readonly static Type Single = typeof(float);
		public readonly static Type Double = typeof(double);

		public readonly static Type DateTime = typeof(DateTime);
		public readonly static Type TimeSpan = typeof(TimeSpan);

		public readonly static Type IList = typeof(IList);
		public readonly static Type IDictionary = typeof(IDictionary);
		public readonly static Type IEnumerable = typeof(IEnumerable);

		//public readonly static Type NullableChar = typeof(Char?);

		//public readonly static Type NullableBoolean = typeof(Boolean?);

		//public readonly static Type NullableSByte = typeof(SByte?);
		//public readonly static Type NullableInt16 = typeof(Int16?);
		//public readonly static Type NullableInt32 = typeof(Int32?);
		//public readonly static Type NullableInt64 = typeof(Int64?);

		//public readonly static Type NullableByte = typeof(Byte?);
		//public readonly static Type NullableUInt16 = typeof(UInt16?);
		//public readonly static Type NullableUInt32 = typeof(UInt32?);
		//public readonly static Type NullableUInt64 = typeof(UInt64?);

		//public readonly static Type NullableDecimal = typeof(Decimal?);
		//public readonly static Type NullableSingle = typeof(Single?);
		//public readonly static Type NullableDouble = typeof(Double?);

		//public readonly static Type NullableDateTime = typeof(DateTime?);
		//public readonly static Type NullableTimeSpan = typeof(TimeSpan?);
	}
}
