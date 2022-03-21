using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Evolution.IOC
{
    public class InstanceLifeTimeProcessor:IDisposable
    {
        private static InstanceLifeTimeProcessor _Instance;
        private static readonly object _lockObj = new object();
        private readonly object _internalLockObj = new object();
        static InstanceLifeTimeProcessor()
        {

        }
        public static InstanceLifeTimeProcessor Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new InstanceLifeTimeProcessor();
                        }
                    }
                }
                return _Instance;
            }
        }
        private InstanceLifeTimeProcessor()
        {
            _singletonCache = new ConcurrentDictionary<string, IService>();
            _scopedCache = new ThreadLocal<ConcurrentDictionary<string, object>>(true);
        }

        private ConcurrentDictionary<string, IService> _singletonCache;
        private ThreadLocal<ConcurrentDictionary<string, object>> _scopedCache;

        public TInterface GetOrCreateSingletonInstance<TInterface>(string key, Func<IService> createServiceFunc) where TInterface : class
        {
            return (_singletonCache.GetOrAdd(key, createServiceFunc())).Instance as TInterface;
        }

        public TInterface GetOrCreateScopedInstance<TInterface>(string key, Func<IService> createServiceFunc) where TInterface : class
        {
            if (_scopedCache.IsValueCreated)
            {
                return (_scopedCache.Value.GetOrAdd(key, createServiceFunc()) as IService).Instance as TInterface;
            }
            else
            {
                _scopedCache.Value = new ConcurrentDictionary<string, object>();
                return (_scopedCache.Value.GetOrAdd(key, createServiceFunc()) as IService).Instance as TInterface;
            }
        }

        public void Dispose()
        {
            lock (_internalLockObj)
            {
                if (_scopedCache.Values.Any())
                {
                    _scopedCache.Values.Clear();
                }
                _singletonCache.Clear();
            }
        }
        public static void Clear()
        {
            Instance.Dispose();
        }
    }
}
