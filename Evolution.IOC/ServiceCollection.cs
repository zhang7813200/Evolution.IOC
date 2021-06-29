using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public class ServiceCollection : IServiceCollection
    {
        private IDictionary<string, IService> _cache;
        public ServiceCollection(IDictionary<string, IService> cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// key:interface fullname  value:implement instance
        /// </summary>
        private ConcurrentDictionary<string, IService> _container = new ConcurrentDictionary<string, IService>();

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public IService this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int IndexOf(IService item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, IService item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public void Add(IService item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(IService item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IService[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(IService item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<IService> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
