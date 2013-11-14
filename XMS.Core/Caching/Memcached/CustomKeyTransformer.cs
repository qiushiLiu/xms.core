using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Enyim;
using Enyim.Caching;
using Enyim.Caching.Memcached;

namespace XMS.Core.Caching.Memcached
{
	public class CustomKeyTransformer : KeyTransformerBase
	{
		private TigerHash th = new TigerHash();

		public CustomKeyTransformer()
		{

		}

		public override string Transform(string key)
		{
			if (key.Length > 127)
			{
				byte[] data = th.ComputeHash(Encoding.Unicode.GetBytes(key));

				return Convert.ToBase64String(data, Base64FormattingOptions.None);
			}

			return key;
		}
	}
}
