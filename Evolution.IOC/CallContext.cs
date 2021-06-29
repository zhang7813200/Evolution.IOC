using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Evolution.IOC
{
    public static class CallContext
    {
        static Dictionary<string, IService> state;
        private static readonly object _lockObj = new object();
        private static Dictionary<int, object> Cache = new Dictionary<int, object>();

        public static void SetData(string name, IService data)
        {
            lock (_lockObj)
            {
                if (state == null) state = new Dictionary<string, IService>();
                if (!Cache.ContainsKey(System.Threading.Thread.CurrentThread.ManagedThreadId))
                {
                    if (!state.ContainsKey(name))
                    {
                        state.Add(name, data);
                    }
                    Cache.Add(System.Threading.Thread.CurrentThread.ManagedThreadId, state);
                }
                else
                {
                    var cacheState =(Cache[System.Threading.Thread.CurrentThread.ManagedThreadId] as Dictionary<string, IService>);
                    if (!cacheState.ContainsKey(name))
                    {
                        cacheState.Add(name, data);
                    }
                }
            }
        }

        public static IService GetData(string name)
        {
            lock (_lockObj)
            {
                if (state == null) state = new Dictionary<string, IService>();
                object obj;
                if (Cache.TryGetValue(System.Threading.Thread.CurrentThread.ManagedThreadId, out obj) && obj != null && obj is Dictionary<string, IService>)
                {
                    IService result = null;
                    var realObj = obj as Dictionary<string, IService>;
                    var exists = realObj.TryGetValue(name, out result);
                    return result;
                }
                return null;
            }
        }

        public static void Clear()
        {
            lock (_lockObj)
            {
                if (state != null)
                {
                    state.Clear();
                }
                if (Cache != null)
                {
                    Cache.Clear();
                }
            }
        }
    }
}
