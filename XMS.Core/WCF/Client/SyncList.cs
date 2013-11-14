using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XMS.Core.WCF.Client
{
    public class SyncList<T>:IDisposable
    {
        private LinkedList<T> lst = null;
        public SyncList()
        {
             this.lst=new LinkedList<T>();
        }
        private object objLock=new object();
        public int Count
        {
            get
            {
                return lst.Count;
            }
        }
        public T Pop()
        {
            lock (objLock)
            {
                if (lst.Count == 0)
                    return default(T);
                LinkedListNode<T> objRslt = lst.First;
                lst.RemoveFirst();
                return objRslt.Value;
            }
        }
        public void Push(T Item)
        {
            lock (objLock)
            {
                lst.AddLast(Item);
            }
        }

        private void DisposeItem(T item)
        {
            if (item is IDisposable)
            {
                ((IDisposable)item).Dispose();
            }
        }
       	#region IDisposable interface
		private bool disposed = false;

		/// <summary>
		/// 释放托管和非托管资源。
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);

			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// 释放非托管资源。
		/// </summary>
		/// <param name="disposing"><b>true</b> 同时释放托管和非托管资源; <b>false</b> 只释放非托管资源。</param>
		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
                    lock (objLock)
                    {
                        foreach (T item in lst)
                        {
                            DisposeItem(item);
                        }
                    }
				}

				this.disposed = true;
			}
		}

		/// <summary>
		/// 析构函数
		/// </summary>
        ~SyncList()
		{
			Dispose(false);
		}
		
		#endregion
    }
}
