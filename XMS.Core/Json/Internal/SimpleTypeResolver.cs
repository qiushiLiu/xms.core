using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	internal class SimpleTypeResolver : JavaScriptTypeResolver
	{
		public override Type ResolveType(string id)
		{
			return Type.GetType(id);
		}

		public override string ResolveTypeId(Type type)
		{
			if (type == null)
			{
				throw new ArgumentNullException("type");
			}
			return type.AssemblyQualifiedName;
		}
	}
}
