using Evolution.IOC.Attribute;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Evolution.IOC
{
    public class IOCContainer : IContainer
    {
        /// <summary>
        /// key:interface fullname  value:implement instance
        /// </summary>
        private ConcurrentDictionary<string, IRegisterInfo> _container = new ConcurrentDictionary<string, IRegisterInfo>();

        private ConcurrentDictionary<string, IService> _SingletonCache = new ConcurrentDictionary<string, IService>();
        private ConcurrentDictionary<string, AsyncLocal<IService>> _ScopedCache= new ConcurrentDictionary<string, AsyncLocal<IService>>();

        public IOCContainer()
        { 
            
        }
        IRegisterInfo IContainer.AddScoped<TInterface, TImplement>()
        {
            var key = GetKey(typeof(TInterface).FullName);
            return _container.GetOrAdd(key, (str)=> {
                return new ImplementInstanceInfo()
                {
                    LifeTime = LifeTime.Scoped,
                    ImplementClassType = typeof(TImplement),
                    // ParameterValues = parameterValues
                    //Instance = parameterValues!=null && parameterValues.Any()?Activator.CreateInstance(typeof(TImplement), parameterValues):Activator.CreateInstance(typeof(TImplement))
                };
            });
        }

        IRegisterInfo IContainer.AddService<TInterface, TImplement>(LifeTime lifeTime)
        {
            var key = GetKey(typeof(TInterface).FullName);
            return _container.GetOrAdd(key, (str) => {
                return new ImplementInstanceInfo()
                {
                    LifeTime = lifeTime,
                    ImplementClassType = typeof(TImplement),
                    //ParameterValues = parameterValues
                    //Instance = parameterValues != null && parameterValues.Any() ? Activator.CreateInstance(typeof(TImplement), parameterValues) : Activator.CreateInstance(typeof(TImplement))
                };
            });
        }

        IRegisterInfo IContainer.AddSingleton<TInterface, TImplement>()
        {
            var key = GetKey(typeof(TInterface).FullName);
            return _container.GetOrAdd(key, (str) => {
                return new ImplementInstanceInfo()
                {
                    LifeTime = LifeTime.Singleton,
                    ImplementClassType = typeof(TImplement),
                    //ParameterValues = parameterValues
                    //Instance = parameterValues != null && parameterValues.Any() ? Activator.CreateInstance(typeof(TImplement), parameterValues) : Activator.CreateInstance(typeof(TImplement))
                };
            });
        }

        IRegisterInfo IContainer.AddTransient<TInterface, TImplement>()
        {
            var key = GetKey(typeof(TInterface).FullName);
            return _container.GetOrAdd(key, (str) => {
                return new ImplementInstanceInfo()
                {
                    LifeTime = LifeTime.Transient,
                    ImplementClassType = typeof(TImplement),
                    //ParameterValues = parameterValues
                    //Instance = parameterValues != null && parameterValues.Any() ? Activator.CreateInstance(typeof(TImplement), parameterValues) : Activator.CreateInstance(typeof(TImplement))
                };
            });
        }

        TInterface IContainer.Resolve<TInterface>()
        {
            var key = GetKey(typeof(TInterface).FullName);
            //是否注册过
            IRegisterInfo registerInfo;
            if (TryGetRegisterInfo(key, out registerInfo))
            {
                IService result = null;
                //判断生命周期
                switch (registerInfo.LifeTime)
                {
                    case LifeTime.Singleton:
                        if (!_SingletonCache.TryGetValue(key, out result))
                        {
                            result = _SingletonCache.GetOrAdd(key, (str) =>
                            {
                                return new ImplementInstanceInfo()
                                {
                                    LifeTime = registerInfo.LifeTime,
                                    Instance = CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay)
                                };
                            });
                        }
                        return result.Instance as TInterface;
                    case LifeTime.Scoped:
                        AsyncLocal<IService> result1 = null;
                        if (!_ScopedCache.TryGetValue(key, out result1))
                        {
                            result1 = _ScopedCache.GetOrAdd(key, (str) =>
                            {
                                return new AsyncLocal<IService>()
                                {
                                    Value = new ImplementInstanceInfo()
                                    {
                                        LifeTime = registerInfo.LifeTime,
                                        Instance = CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay)
                                    }
                                };
                            });
                        }
                        return result1.Value.Instance as TInterface;
                    case LifeTime.Transient:
                        return CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay) as TInterface;
                    default:
                        break;
                }
                return result != null ? (result.Instance as TInterface) : null;
            }

            return null;
        }

        private bool TryGetRegisterInfo(string key, out IRegisterInfo registerInfo)
        {
            return _container.TryGetValue(key, out registerInfo) && registerInfo != null && registerInfo.ImplementClassType != null;
        }

        private bool TryFuzzyGetRegisterInfo(string key, Func<string,bool> fuzzyCondition, out IList<IRegisterInfo> registerInfos)
        {
            var conditionKeys = _container.Keys.Where(it => fuzzyCondition(key));
            if (conditionKeys != null && conditionKeys.Any())
            {
                registerInfos = new List<IRegisterInfo>();
                foreach (var conditionKey in conditionKeys)
                {
                    IRegisterInfo registerInfo;
                    _container.TryGetValue(conditionKey, out registerInfo);
                    registerInfos.Add(registerInfo);
                }
                return registerInfos.Any();
            }
            registerInfos = null;
            return false;
        }
        

        private static string GetKey(string interfaceFullName, params object[] parameterValues)
        {
            if (parameterValues != null && parameterValues.Any())
            {
                foreach (object obj in parameterValues)
                {
                    interfaceFullName = $"{interfaceFullName}{obj.ToString()}";
                }
            }
            return System.Text.Encoding.UTF8.GetString(MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(interfaceFullName)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">实现类 type</param>
        /// <param name="parameterValues">实现类创建所需参数</param>
        /// <param name="injectionWay">实现类内, 需要依赖注入的接口所使用的注入方式(用特性附着在构造器，方法或者属性上，以标识注入方式)</param>
        /// <returns></returns>
        private object CreateObject(Type type, object[] parameterValues=null, InjectionWay injectionWay= InjectionWay.None)
        {
            ConstructorInfo constructorInfo = null;
            MethodInfo methodInfo = null;
            IEnumerable<PropertyInfo> propertiesInfo= null;

            //给当前类进行实例化
            //var instance =parameterValues != null && parameterValues.Any() ? Activator.CreateInstance(type, parameterValues) : Activator.CreateInstance(type); 

            //如果没有指定注入方式，
            if (injectionWay == InjectionWay.None)
            {
                // 按照构造器 → 属性 → 方法，查找可能是接口的或者含有接口参数的数据
                //1. 构造器: 尝试查找这个type, 参数最多的构造器
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null && constructor.GetParameters().Length > 0)
                {
                    var interfaceParamValues = GetConstructorParamValues(constructor);
                    return Activator.CreateInstance(type, interfaceParamValues.ToArray());
                }
                //2. 属性: 尝试查找这个type下,所有定义为Interface的并且值为空的变量
                else if (type.GetProperties().Any(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null && it.GetGetMethod().Invoke(type, null) == null))
                {
                    var properties = type.GetProperties().Where(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null && it.GetGetMethod().Invoke(type, null) == null);
                    var instance = Activator.CreateInstance(type, parameterValues);
                    SetInstanceProperties(instance, properties);
                    return instance;
                }
                //3. 方法: 和构造器一样，只查找参数最多的
                else if (type.GetMethods().Where(it => it.IsStatic == false && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface)).OrderByDescending(it => it.GetParameters().Length).FirstOrDefault() != null)
                {
                    MethodInfo method = GetMethodWhichHasMostParametersCount(type);
                    IList<object> interfaceParamValues = GetMethodParamValues(method);
                    if (interfaceParamValues != null && interfaceParamValues.Any())
                    {
                        var instance = Activator.CreateInstance(type, parameterValues);
                        method.Invoke(instance, interfaceParamValues.ToArray());
                        return instance;
                    }
                }
            }
            else
            {
                //根据InjectionWay 找指定的即可
                switch (injectionWay)
                {
                    case InjectionWay.Constructor:
                        var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                        if (constructor != null)
                        {
                            var interfaceParamValues = GetConstructorParamValues(constructor);
                            return Activator.CreateInstance(type, interfaceParamValues.ToArray());
                        }
                        break;
                    case InjectionWay.Property:
                        var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                        if (properties != null && properties.Any())
                        {
                            var instance = Activator.CreateInstance(type, parameterValues);
                            SetInstanceProperties(instance, properties);
                            return instance;
                        }
                        break;
                    case InjectionWay.Method:
                        var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                        if (method != null)
                        {
                            IList<object> interfaceParamValues = GetMethodParamValues(method);
                            if (interfaceParamValues != null && interfaceParamValues.Any())
                            {
                                var instance = Activator.CreateInstance(type, parameterValues);
                                method.Invoke(instance, interfaceParamValues.ToArray());
                                return instance;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">实现类 type</param>
        /// <param name="parameterValues">实现类创建所需参数</param>
        /// <param name="injectionWay">实现类内, 需要依赖注入的接口所使用的注入方式(用特性附着在构造器，方法或者属性上，以标识注入方式)</param>
        /// <returns></returns>
        private object CreateObjectV1(Type type, object[] parameterValues = null, InjectionWay injectionWay = InjectionWay.None)
        {
            ConstructorInfo filteredConstructorInfo = null;
            MethodInfo filteredMethodInfo = null;
            IEnumerable<PropertyInfo> filteredPropertiesInfo = null;
            if (injectionWay == InjectionWay.None)
            {
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null && constructor.GetParameters().Length > 0)
                {
                    filteredConstructorInfo = constructor;
                }

                var properties = type.GetProperties().Where(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null && it.GetGetMethod().Invoke(type, null) == null);
                if (properties != null && properties.Any())
                {
                    filteredPropertiesInfo = properties;
                }

                var method = type.GetMethods().Where(it => it.IsStatic == false && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface)).OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
                if (method != null && method.GetParameters().Length > 0)
                {
                    filteredMethodInfo = method;
                }
            }
            else
            {
                //根据InjectionWay 找指定的即可
                switch (injectionWay)
                {
                    case InjectionWay.Constructor:
                        var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                        if (constructor != null)
                        {
                            filteredConstructorInfo = constructor;
                        }
                        break;
                    case InjectionWay.Property:
                        var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                        if (properties != null && properties.Any())
                        {
                            filteredPropertiesInfo = properties;
                        }
                        break;
                    case InjectionWay.Method:
                        var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                        if (method != null)
                        {
                            filteredMethodInfo = method;
                        }
                        break;
                    default:
                        break;
                }
            }

            //根据InjectionWay 找指定的即可
            switch (injectionWay)
                {
                    case InjectionWay.Constructor:
                        var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                        if (constructor != null)
                        {
                            return CreateInstanceForConstructorInjection(type, constructor);
                        }
                        break;
                    case InjectionWay.Property:
                        var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                        if (properties != null && properties.Any())
                        {
                            return CreateInstanceForPropertyInjection(type, properties,parameterValues);
                        }
                        break;
                    case InjectionWay.Method:
                        var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                        if (method != null)
                        {
                            return CreateInstanceForMethodInjection(type, method, parameterValues);
                        }
                        break;
                    default:
                        break;
                }
            
            return null;
        }

        private object CreateObjectV2(Type type, object[] parameterValues = null, InjectionWay injectionWay = InjectionWay.None)
        {
            ConstructorInfo filteredConstructorInfo = null;
            MethodInfo filteredMethodInfo = null;
            IEnumerable<PropertyInfo> filteredPropertiesInfo = null;

            #region 默认走构造器注入方式
            if (injectionWay.HasFlag(InjectionWay.None))
            {
                object instance = null;
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null && constructor.GetParameters().Length > 0)
                {
                    filteredConstructorInfo = constructor;
                    instance = CreateInstanceForConstructorInjection(type, filteredConstructorInfo);
                }
                return instance;
            }
            #endregion

            #region 指定某种注入方式

            if (injectionWay.HasFlag(InjectionWay.Constructor))
            {
                var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                if (constructor != null)
                {
                    return CreateInstanceForConstructorInjection(type, constructor);
                }
            }

            if (injectionWay.HasFlag(InjectionWay.Property))
            {
                var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                if (properties != null && properties.Any())
                {
                    return CreateInstanceForPropertyInjection(type, properties, parameterValues);
                }
            }

            if (injectionWay.HasFlag(InjectionWay.Method))
            {
                var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                if (method != null)
                {
                    return CreateInstanceForMethodInjection(type, method, parameterValues);
                }
            }

            #endregion

            #region 兼容模式注入,需要特性标识,才可注入成功
            if (injectionWay.HasFlag(InjectionWay.All))
            {
                object instance = null;
                //如果有构造器先把 instance建出来
                var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                if (constructor != null)
                {
                    instance =  CreateInstanceForConstructorInjection(type, constructor);
                }


                var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                if (properties != null && properties.Any())
                {
                    filteredPropertiesInfo = properties;
                    if (instance != null)
                    {
                        SetInstanceProperties(instance, properties);
                    }
                    else
                    {

                        instance = CreateInstanceForPropertyInjection(type, properties, parameterValues);
                    }
                }

                var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                if (method != null && method.GetParameters().Length > 0)
                {
                    filteredMethodInfo = method;
                    if (instance != null)
                    {
                        InvokeInstanceMethod(instance, method);
                    }
                    else
                    {

                        instance = CreateInstanceForMethodInjection(type, filteredMethodInfo, parameterValues);
                    }
                }
                return instance;
            }
            #endregion
            
            #region 智能模式注入: 无需特性标识, 也可注入成功
            if (injectionWay.HasFlag(InjectionWay.Smart))
            {
                object instance = null;
                //如果有构造器先把 instance建出来
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null && constructor.GetParameters().Length > 0)
                {
                    filteredConstructorInfo = constructor;
                    instance = CreateInstanceForConstructorInjection(type, filteredConstructorInfo);
                }


                var properties = type.GetProperties().Where(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null && it.GetGetMethod().Invoke(type, null) == null);
                if (properties != null && properties.Any())
                {
                    filteredPropertiesInfo = properties;
                    if (instance != null)
                    {
                        SetInstanceProperties(instance, properties);
                    }
                    else
                    {

                        instance = CreateInstanceForPropertyInjection(type, properties, parameterValues);
                    }
                }

                var method = type.GetMethods().Where(it => it.IsStatic == false && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface)).OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
                if (method != null && method.GetParameters().Length > 0)
                {
                    filteredMethodInfo = method;
                    if (instance != null)
                    {
                        InvokeInstanceMethod(instance, method);
                    }
                    else
                    {

                        instance = CreateInstanceForMethodInjection(type, filteredMethodInfo, parameterValues);
                    }
                }
                return instance;
            }
            #endregion

            return null;
        }

        private object CreateInstanceForPropertyInjection(Type type, IEnumerable<PropertyInfo> properties, object[] parameterValues =null)
        {
            var instance = Activator.CreateInstance(type, parameterValues);
            SetInstanceProperties(instance, properties);
            return instance;
        }

        private void UpdateInstanceForPropertyInjection(Object instance, IEnumerable<PropertyInfo> properties, object[] parameterValues = null)
        {
            SetInstanceProperties(instance, properties);
        }

        private object CreateInstanceForConstructorInjection(Type type, ConstructorInfo constructor)
        {
            var interfaceParamValues = GetConstructorParamValues(constructor);
            if (interfaceParamValues != null && interfaceParamValues.Any())
            {
                return Activator.CreateInstance(type, interfaceParamValues.ToArray());
            }
            return null;
        }

        private object CreateInstanceForMethodInjection(Type type, MethodInfo method, object[] parameterValues = null)
        {
            IList<object> interfaceParamValues = GetMethodParamValues(method);
            if (interfaceParamValues != null && interfaceParamValues.Any())
            {
                var instance = Activator.CreateInstance(type, parameterValues);
                method.Invoke(instance, interfaceParamValues.ToArray());
                return instance;
            }
            return null;
        }

        private void InvokeInstanceMethod(object instance, MethodInfo method, object[] parameterValues = null)
        {
            IList<object> interfaceParamValues = GetMethodParamValues(method);
            if (interfaceParamValues != null && interfaceParamValues.Any())
            {
                method.Invoke(instance, interfaceParamValues.ToArray());
            }
        }
        

        private IList<object> GetConstructorParamValues(ConstructorInfo constructor)
        {
            IList<object> interfaceParamValues = new List<object>();
            foreach (var parameter in constructor.GetParameters())
            {
                //判断这个实现类的构造器里参数是否是接口,
                if (parameter.ParameterType.IsInterface)
                {
                    var key = GetKey(parameter.ParameterType.FullName);
                    IRegisterInfo registerInfo = null;
                    if (TryGetRegisterInfo(key, out registerInfo) && registerInfo != null && registerInfo.ImplementClassType != null)
                    {
                        object currentParamValue = CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
                        interfaceParamValues.Add(currentParamValue);
                    }
                }
                else
                {
                    //构造器里可能有一些不是用来做IOC的参数，直接赋值成默认值
                    if (parameter.HasDefaultValue)
                    {
                        interfaceParamValues.Add(parameter.DefaultValue);
                    }
                    else
                    {
                        interfaceParamValues.Add(null);
                    }
                }
            }

            return interfaceParamValues;
        }

        private IList<object> GetMethodParamValues(MethodInfo method)
        {
            IList<object> interfaceParamValues = new List<object>();
            foreach (var parameter in method.GetParameters())
            {
                //判断这个实现类的构造器里参数是否是接口,
                if (parameter.ParameterType.IsInterface)
                {
                    var key = GetKey(parameter.ParameterType.FullName);
                    IRegisterInfo registerInfo = null;
                    if (TryGetRegisterInfo(key, out registerInfo) && registerInfo != null && registerInfo.ImplementClassType != null)
                    {
                        object currentParamValue = CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
                        interfaceParamValues.Add(currentParamValue);
                    }
                }
                else
                {
                    //构造器里可能有一些不是用来做IOC的参数，直接赋值成默认值
                    if (parameter.HasDefaultValue)
                    {
                        interfaceParamValues.Add(parameter.DefaultValue);
                    }
                    else
                    {
                        interfaceParamValues.Add(null);
                    }
                }
            }

            return interfaceParamValues;
        }

        private void SetInstanceProperties(Object instance, IEnumerable<PropertyInfo> conditionProperties)
        {
            //var conditionProperties = type.GetProperties().Where(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null && it.GetGetMethod().Invoke(type, null) == null);
            foreach (PropertyInfo interfaceProperty in conditionProperties)
            {
                IRegisterInfo registerInfo;
                if (TryGetRegisterInfo(interfaceProperty.PropertyType, out registerInfo))
                {
                    var value = CreateObject(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
                    interfaceProperty.SetValue(instance, value);
                }
            }
        }

        private object CreateInstanceByConstructor(Type type, ConstructorInfo constructor)
        {
            var interfaceParamValues = GetConstructorParamValues(constructor);
            return Activator.CreateInstance(type, interfaceParamValues.ToArray());
        }

        private bool TryGetImpleType(Type interfaceType, out Type impleType)
        {
            var key = GetKey(interfaceType.FullName);
            IRegisterInfo registerInfo = null;
            if (TryGetRegisterInfo(key, out registerInfo) && registerInfo != null && registerInfo.ImplementClassType != null)
            {
                impleType = registerInfo.ImplementClassType;
                return true;
            }
            impleType = null;
            return false;
        }

        private bool TryGetRegisterInfo(Type interfaceType, out IRegisterInfo registerInfo)
        {
            var key = GetKey(interfaceType.FullName);
            return TryGetRegisterInfo(key, out registerInfo) && registerInfo != null;
        }

        private static System.Reflection.ConstructorInfo GetConstructorWhichHasMostParametersCount(Type type)
        {
            return type.GetConstructors().OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
        }

        private static System.Reflection.MethodInfo GetMethodWhichHasMostParametersCount(Type type)
        {
            return type.GetMethods().Where(it => it.IsStatic == false && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface)).OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();//type.GetMethods().OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
        }

        private static bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructors().Any(it => it.GetParameters().Length == 0);
        }

        void ISyncConfig.Sync(IBaseServiceProvider serviceProvider, LifeTime lifeTime = LifeTime.Transient)
        {
            throw new NotImplementedException();
        }

        IServiceCollection ISyncConfig.SyncJsonConfig(string jsonConfigLocalPath, LifeTime lifeTime = LifeTime.Transient)
        {
            IServiceSyncWorker syncWorker = new XmlServiceSyncWorker(jsonConfigLocalPath);
            if (syncWorker.HasValue)
            {
                foreach (var item in syncWorker.Result)
                {
                    if (!_container.ContainsKey(item.Key))
                    {
                        _container.TryAdd(item.Key, item.Value);
                    }
                }
            }
            return new ServiceCollection(syncWorker.Result);
        }

        IServiceCollection ISyncConfig.SyncXmlConfig(string xmlConfigLocalPath, LifeTime lifeTime = LifeTime.Transient)
        {
            throw new NotImplementedException();
        }
    }
}
