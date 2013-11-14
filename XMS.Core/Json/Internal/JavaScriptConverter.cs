using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	internal abstract class JavaScriptConverter
	{
		protected JavaScriptConverter()
		{
		}

		public abstract object Deserialize(IDictionary<string, object> dictionary, Type type, JavaScriptSerializer serializer, string[] extraTimeFormats);
		public abstract IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer);

		public abstract IEnumerable<Type> SupportedTypes { get; }
	}
}
