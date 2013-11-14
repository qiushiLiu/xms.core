using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.Json
{
	internal class JavaScriptString
	{
		private int _index;
		private string _s;

		internal JavaScriptString(string s)
		{
			this._s = s;
		}

		internal string GetDebugString(string message)
		{
			return string.Concat(new object[] { message, " (", this._index, "): ", this._s });
		}

		internal char? GetNextNonEmptyChar()
		{
			while (this._s.Length > this._index)
			{
				char c = this._s[this._index++];
				if (!char.IsWhiteSpace(c))
				{
					return new char?(c);
				}
			}
			return null;
		}

		internal char? MoveNext()
		{
			if (this._s.Length > this._index)
			{
				return new char?(this._s[this._index++]);
			}
			return null;
		}

		internal string MoveNext(int count)
		{
			if (this._s.Length >= (this._index + count))
			{
				string str = this._s.Substring(this._index, count);
				this._index += count;
				return str;
			}
			return null;
		}

		internal void MovePrev()
		{
			if (this._index > 0)
			{
				this._index--;
			}
		}

		internal void MovePrev(int count)
		{
			while ((this._index > 0) && (count > 0))
			{
				this._index--;
				count--;
			}
		}

		public override string ToString()
		{
			if (this._s.Length > this._index)
			{
				return this._s.Substring(this._index);
			}
			return string.Empty;
		}

		// 我们的实现
		public string SubString(int maxCount)
		{
			if (this._s.Length > this._index)
			{
				return this._s.Substring(this._index, Math.Min(maxCount, this._s.Length - this._index));
			}
			return string.Empty;
		}

		public bool CheckNextNonEmptyCharIsC(char c)
		{
			int i = this._index;
			while (i < this._s.Length)
			{
				if (!char.IsWhiteSpace(this._s[i]))
				{
					if (c != this._s[i])
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				i++;
			}
			return false;
		}
	}
}
