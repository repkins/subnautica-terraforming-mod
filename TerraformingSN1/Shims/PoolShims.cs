using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terraforming.Shims
{
    public static class Pool<T> where T: new()
    {
        public static T Get()
        {
            return new T();
        }
    }

    public class ListPool<T>: IDisposable
    {
        private bool disposedValue;

        public List<T> list = new List<T>();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    list.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
