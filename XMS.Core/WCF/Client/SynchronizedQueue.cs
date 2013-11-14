using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace XMS.Core.WCF.Client
{
	internal sealed class System_QueueDebugView<T>
	{
		private Queue<T> queue;
		public System_QueueDebugView(Queue<T> queue)
		{
			if (queue == null)
			{
				throw new ArgumentNullException("queue");
			}
			this.queue = queue;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return this.queue.ToArray();
			}
		}
	}

	[Serializable]
	[ComVisible(false)]
	[DebuggerTypeProxy(typeof(System_QueueDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public class SynchronizedQueue<T> : IEnumerable<T>, ICollection, IEnumerable
	{
		protected Queue<T> queue;
		protected object root;

		/// <summary>
		/// 初始化 SynchronizedQueue<T> 类的新实例，该实例为空并且具有默认初始容量。
		/// </summary>
		/// <param name="q"></param>
		public SynchronizedQueue(Queue<T> q)
		{
			this.queue = q;
			this.root = ((ICollection)q).SyncRoot;
		}

		#region ICollection 接口的实现
		/// <summary>
		/// 获取 SynchronizedQueue<T> 中包含的元素数。
		/// </summary>
		/// <value>SynchronizedQueue<T> 中包含的元素数。</value>
		public int Count
		{
			get
			{
				lock (this.root)
				{
					return this.queue.Count;
				}
			}
		}

		/// <summary>
		/// 获取一个值，该值指示是否同步对 SynchronizedQueue<T> 的访问（线程安全）。 
		/// </summary>
		public bool IsSynchronized
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// 获取可用于同步 SynchronizedQueue<T> 访问的对象。 
		/// </summary>
		public object SyncRoot
		{
			get
			{
				return this.root;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			lock (this.root)
			{
				((ICollection)this.queue).CopyTo(array, index);
			}
		}

		/// <summary>
		/// 从指定数组索引开始将 System.Collections.Generic.Queue<T> 元素复制到现有一维数组中。
		/// </summary>
		/// <param name="array"> 作为从 System.Collections.Generic.Queue<T> 复制的元素的目标位置的一维数组。必须具有从零开始的索引。</param>
		/// <param name="arrayIndex">数组中从零开始的索引，将在此处开始复制。</param>
		/// <exception cref="System.ArgumentNullException">array 为 null。</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">arrayIndex 小于零。</exception>
		/// <exception cref="System.ArgumentException">源 SynchronizedQueue<T> 中的元素数目大于从 arrayIndex 到目标 array 末尾之间的可用空间。</exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (this.root)
			{
				this.queue.CopyTo(array, arrayIndex);
			}
		}
		#endregion


		/// <summary>
		/// 从 SynchronizedQueue<T> 中移除所有对象。
		/// </summary>
		public void Clear()
		{
			lock (this.root)
			{
				this.queue.Clear();
			}
		}

		/// <summary>
		/// 确定某元素是否在 SynchronizedQueue<T> 中。
		/// </summary>
		/// <param name="item">要在 SynchronizedQueue<T> 中定位的对象。对于引用类型，该值可以为 null。</param>
		/// <returns>如果在 SynchronizedQueue<T> 中找到 item，则为 true；否则为 false。</returns>
		public bool Contains(T item)
		{
			lock (this.root)
			{
				return this.queue.Contains(item);
			}
		}


		/// <summary>
		/// 移除并返回位于 SynchronizedQueue<T> 开始处的对象。
		/// </summary>
		/// <returns>从 SynchronizedQueue<T> 的开头移除的对象。</returns>
		/// <exception cref="System.InvalidOperationException">SynchronizedQueue<T> 为空。</exception>
		public T Dequeue()
		{
			lock (this.root)
			{
				return this.queue.Dequeue();
			}
		}

		/// <summary>
		/// 将对象添加到 SynchronizedQueue<T> 的结尾处。
		/// </summary>
		/// <param name="item">要添加到 SynchronizedQueue<T> 的对象。对于引用类型，该值可以为 null。</param>
		public void Enqueue(T item)
		{
			lock (this.root)
			{
				this.queue.Enqueue(item);
			}
		}

		/// <summary>
		/// 返回位于 SynchronizedQueue<T> 开始处的对象但不将其移除。
		/// </summary>
		/// <returns>位于 SynchronizedQueue<T> 的开头的对象。</returns>
		/// <exception cref="System.InvalidOperationException">SynchronizedQueue<T> 为空。</exception>
		public T Peek()
		{
			lock (this.root)
			{
				return this.queue.Peek();
			}
		}

		/// <summary>
		/// 将 SynchronizedQueue<T> 元素复制到新数组。
		/// </summary>
		/// <returns>包含从 SynchronizedQueue<T> 复制的元素的新数组。</returns>
		public T[] ToArray()
		{
			lock (this.root)
			{
				return this.queue.ToArray();
			}
		}

		/// <summary>
		/// 如果元素数小于当前容量的 90%，将容量设置为 SynchronizedQueue<T> 中的实际元素数。
		/// </summary>
		public void TrimExcess()
		{
			lock (this.root)
			{
				this.queue.TrimExcess();
			}
		}


		#region IEnumerable<T>、IEnumerable 接口的实现
		/// <summary>
		/// 返回循环访问 SynchronizedQueue&lt;T&gt; 的枚举数。
		/// </summary>
		/// <returns>用于 SynchronizedQueue&lt;T&gt; 的 System.Collections.Generic.Queue&lt;T&gt;.Enumerator。</returns>
		public Queue<T>.Enumerator GetEnumerator()
		{
			lock (this.root)
			{
				return this.queue.GetEnumerator();
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			lock (this.root)
			{
				return ((IEnumerable<T>)this.queue).GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			lock (this.root)
			{
				return ((IEnumerable)this.queue).GetEnumerator();
			}
		}
		#endregion
	}
}