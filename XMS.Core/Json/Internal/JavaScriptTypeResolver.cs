using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	internal abstract class JavaScriptTypeResolver
	{
		protected JavaScriptTypeResolver()
		{
		}

		public abstract Type ResolveType(string id);
		public abstract string ResolveTypeId(Type type);
	}
}
