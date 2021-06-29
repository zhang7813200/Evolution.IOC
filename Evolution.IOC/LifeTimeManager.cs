using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Evolution.IOC
{
    public interface IInstanceLifeTimeProcessor
    {
        object CacheInstance { get; }
        object NewInstance { get; }
        bool CanUseCache(Type type);
    }

    public class SingletonInstanceLifeTimeProcessor : IInstanceLifeTimeProcessor
    {
        private ConcurrentDictionary<string, IService> _singletonCache;
        public SingletonInstanceLifeTimeProcessor()
        {
            _singletonCache = new ConcurrentDictionary<string, IService>();
        }
        public object CacheInstance {
            get
            {
                return null;
            }
        }

        public object NewInstance {
            get
            {
                return null;
            }
        }

        public bool CanUseCache(Type type)
        {
            IService service;
            return _singletonCache.ContainsKey(type.FullName) && _singletonCache.TryGetValue(type.FullName, out service);
        }
    }

    public class ScopedInstanceLifeTimeProcessor : IInstanceLifeTimeProcessor
    {
        private ConcurrentDictionary<string, AsyncLocal<IService>> _scopedCache;
        public ScopedInstanceLifeTimeProcessor()
        {
            _scopedCache = new ConcurrentDictionary<string, AsyncLocal<IService>>();
        }
        public object CacheInstance => throw new NotImplementedException();

        public object NewInstance => throw new NotImplementedException();

        public bool CanUseCache(Type type)
        {
            throw new NotImplementedException();
        }
    }

    public class TransientInstanceLifeTimeProcessor : IInstanceLifeTimeProcessor
    {
        //public object CacheInstance {
        //    get
        //    {
        //        return null;
        //    }
        //}

        //public object NewInstance {
        //    get
        //    {
        //        return null;
        //    }
        //}

        //public bool CanUseCache(Type type)
        //{
        //    Activator.CreateInstance();
        //    return false;
        //}
        public object CacheInstance => throw new NotImplementedException();

        public object NewInstance => throw new NotImplementedException();

        public bool CanUseCache(Type type)
        {
            throw new NotImplementedException();
        }
    }

    public class InstanceLifeTimeManager
    {
        private IInstanceLifeTimeProcessor _processor;
        public InstanceLifeTimeManager(LifeTime lifeTime)
        {
            InitLifeTimeProcesser(lifeTime);
        }

        private void InitLifeTimeProcesser(LifeTime lifeTime)
        {
            //doing sth
            if (_processor == null)
            {
                throw new ArgumentNullException("IInstanceLifeTimeProcessor");
            }
        }

        public InstanceLifeTimeManager(IInstanceLifeTimeProcessor processor)
        {
            if (_processor == null)
            {
                throw new ArgumentNullException("IInstanceLifeTimeProcessor");
            }
            _processor = processor;
        }

        public TInterface Manage<TInterface>() where TInterface : class
        {
            if (_processor.CanUseCache(typeof(TInterface)))
            {
                return _processor.CacheInstance as TInterface;
            }
            else
            {
                return _processor.NewInstance as TInterface;
            }
        }
    }
}
