using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Globalization;

namespace XMS.Core.Json
{
	internal class AtlasWeb
	{
		private static ResourceManager resourceMan = null;

		private static CultureInfo resourceCulture;
 
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					resourceMan = new ResourceManager("System.Web.Resources.AtlasWeb", typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly);
				}
				return resourceMan;
			}
		}

		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string JSON_ArrayTypeNotSupported
		{
			get
			{
				return ResourceManager.GetString("JSON_ArrayTypeNotSupported", resourceCulture);
			}
		}

		internal static string JSON_BadEscape
		{
			get
			{
				return ResourceManager.GetString("JSON_BadEscape", resourceCulture);
			}
		}

		internal static string JSON_CannotConvertObjectToType
		{
			get
			{
				return ResourceManager.GetString("JSON_CannotConvertObjectToType", resourceCulture);
			}
		}

		internal static string JSON_CannotCreateListType
		{
			get
			{
				return ResourceManager.GetString("JSON_CannotCreateListType", resourceCulture);
			}
		}

		internal static string JSON_CircularReference
		{
			get
			{
				return ResourceManager.GetString("JSON_CircularReference", resourceCulture);
			}
		}

		internal static string JSON_DepthLimitExceeded
		{
			get
			{
				return ResourceManager.GetString("JSON_DepthLimitExceeded", resourceCulture);
			}
		}

		internal static string JSON_DeserializerTypeMismatch
		{
			get
			{
				return ResourceManager.GetString("JSON_DeserializerTypeMismatch", resourceCulture);
			}
		}

		internal static string JSON_DictionaryTypeNotSupported
		{
			get
			{
				return ResourceManager.GetString("JSON_DictionaryTypeNotSupported", resourceCulture);
			}
		}

		internal static string JSON_ExpectedOpenBrace
		{
			get
			{
				return ResourceManager.GetString("JSON_ExpectedOpenBrace", resourceCulture);
			}
		}

		internal static string JSON_IllegalPrimitive
		{
			get
			{
				return ResourceManager.GetString("JSON_IllegalPrimitive", resourceCulture);
			}
		}

		internal static string JSON_InvalidArrayEnd
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidArrayEnd", resourceCulture);
			}
		}

		internal static string JSON_InvalidArrayExpectComma
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidArrayExpectComma", resourceCulture);
			}
		}

		internal static string JSON_InvalidArrayExtraComma
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidArrayExtraComma", resourceCulture);
			}
		}

		internal static string JSON_InvalidArrayStart
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidArrayStart", resourceCulture);
			}
		}

		internal static string JSON_InvalidEnumType
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidEnumType", resourceCulture);
			}
		}

		internal static string JSON_InvalidMaxJsonLength
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidMaxJsonLength", resourceCulture);
			}
		}

		internal static string JSON_InvalidMemberName
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidMemberName", resourceCulture);
			}
		}

		internal static string JSON_InvalidObject
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidObject", resourceCulture);
			}
		}

		internal static string JSON_InvalidRecursionLimit
		{
			get
			{
				return ResourceManager.GetString("JSON_InvalidRecursionLimit", resourceCulture);
			}
		}

		internal static string JSON_MaxJsonLengthExceeded
		{
			get
			{
				return ResourceManager.GetString("JSON_MaxJsonLengthExceeded", resourceCulture);
			}
		}

		internal static string JSON_NoConstructor
		{
			get
			{
				return ResourceManager.GetString("JSON_NoConstructor", resourceCulture);
			}
		}

		internal static string JSON_StringNotQuoted
		{
			get
			{
				return ResourceManager.GetString("JSON_StringNotQuoted", resourceCulture);
			}
		}

		internal static string JSON_UnterminatedString
		{
			get
			{
				return ResourceManager.GetString("JSON_UnterminatedString", resourceCulture);
			}
		}

		internal static string JSON_ValueTypeCannotBeNull
		{
			get
			{
				return ResourceManager.GetString("JSON_ValueTypeCannotBeNull", resourceCulture);
			}
		}

	}
}
