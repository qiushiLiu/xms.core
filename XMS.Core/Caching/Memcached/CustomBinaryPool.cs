using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Security;

using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Collections;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Protocol.Binary;
using Enyim.Caching.Memcached.Results.Extensions;

namespace XMS.Core.Caching.Memcached
{
	/// <summary>
	/// Server pool implementing the binary protocol.
	/// </summary>
	internal class CustomBinaryPool : DefaultServerPool
	{
		ISaslAuthenticationProvider authenticationProvider;
		IMemcachedClientConfiguration configuration;

		public CustomBinaryPool(IMemcachedClientConfiguration configuration)
			: base(configuration, new BinaryOperationFactory())
		{
			this.authenticationProvider = GetProvider(configuration);
			this.configuration = configuration;
		}

		protected override IMemcachedNode CreateNode(IPEndPoint endpoint)
		{
			return new CustomBinaryNode(endpoint, this.configuration.SocketPool, this.authenticationProvider);
		}

		private static ISaslAuthenticationProvider GetProvider(IMemcachedClientConfiguration configuration)
		{
			// create&initialize the authenticator, if any
			// we'll use this single instance everywhere, so it must be thread safe
			IAuthenticationConfiguration auth = configuration.Authentication;
			if (auth != null)
			{
				Type t = auth.Type;
				var provider = (t == null) ? null : Enyim.Reflection.FastActivator.Create(t) as ISaslAuthenticationProvider;

				if (provider != null)
				{
					provider.Initialize(auth.Parameters);
					return provider;
				}
			}

			return null;
		}

	}

}
