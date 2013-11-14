using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace XMS.Core.Caching
{
	[Serializable]
	internal class DataCacheItemVersion : IComparable<DataCacheItemVersion>, ISerializable
	{
		// Fields
		private Guid _internalVersion;

		protected Guid Value
		{
			get
			{
				return this._internalVersion;
			}
			set
			{
				this._internalVersion = value;
			}
		}

		public DataCacheItemVersion(Guid version)
		{
			this._internalVersion = version;
		}

		public int CompareTo(DataCacheItemVersion other)
		{
			if (object.Equals(other, null))
			{
				return -1;
			}
			return this._internalVersion.CompareTo(other._internalVersion);
		}

		public override bool Equals(object obj)
		{
			if (object.Equals(obj, null))
			{
				return false;
			}
			return (((DataCacheItemVersion)obj)._internalVersion == this._internalVersion);
		}

		public override int GetHashCode()
		{
			return this._internalVersion.GetHashCode();
		}

		public static bool IsEmpty(DataCacheItemVersion version)
		{
			if (version != null)
			{
				return (version._internalVersion == Guid.Empty);
			}
			return true;
		}

		public static bool operator ==(DataCacheItemVersion left, DataCacheItemVersion right)
		{
			return ((object.Equals(left, null) && object.Equals(right, null)) || (!object.Equals(left, null) && left.Equals(right)));
		}

		public static bool operator >(DataCacheItemVersion left, DataCacheItemVersion right)
		{
			if (object.Equals(left, null) && object.Equals(right, null))
			{
				return false;
			}
			return (!object.Equals(left, null) && (left.CompareTo(right) > 0));
		}

		public static bool operator !=(DataCacheItemVersion left, DataCacheItemVersion right)
		{
			return !(left == right);
		}

		public static bool operator <(DataCacheItemVersion left, DataCacheItemVersion right)
		{
			if (object.Equals(left, null) && object.Equals(right, null))
			{
				return false;
			}
			if (!object.Equals(left, null))
			{
				return (left.CompareTo(right) < 0);
			}
			return true;
		}

		protected DataCacheItemVersion(SerializationInfo info, StreamingContext context)
		{
			this._internalVersion = (Guid)info.GetValue("value", typeof(Guid));
		}

		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("value", this._internalVersion, typeof(Guid));
		}
	}
}
