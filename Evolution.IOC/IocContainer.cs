using Evolution.IOC.Attribute;
using Evolution.IOC.Exception;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        private ConcurrentDictionary<string, IRegisterInfo> _container;
        private ConcurrentDictionary<string, IService> _singletonCache;
        [ThreadStatic]
        private IDictionary<string,IService> _scopedCache;

        private static IContainer _Instance;
        private static readonly object _lockObj = new object();
        static IOCContainer()
        {
            
        }
        public static IContainer Instance {
            get
            {
                if (_Instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_Instance == null)
                        {
                            _Instance = new IOCContainer();
                        }
                    }
                }
                return _Instance;
            }
        }
        private IOCContainer()
        {
            _container = new ConcurrentDictionary<string, IRegisterInfo>();
            _singletonCache = new ConcurrentDictionary<string, IService>();
            _scopedCache = new Dictionary<string, IService>();
        }
        IRegisterInfo IContainer.AddScoped<TInterface, TImplement>()
        {
            var key = GetKey(typeof(TInterface).FullName);
            CheckIfExists(key);
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

        IRegisterInfo IContainer.AddService<TInterface, TImplement>(LifeTime lifeTime = LifeTime.Transient)
        {
            var key = GetKey(typeof(TInterface).FullName);
            CheckIfExists(key);
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
            CheckIfExists(key);
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
            CheckIfExists(key);
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
                //InstanceLifeTimeManager lifeTimeManager = new InstanceLifeTimeManager(registerInfo.LifeTime);
                //return lifeTimeManager.Manage<TInterface>();
                IService result = null;
                //判断生命周期
                switch (registerInfo.LifeTime)
                {
                    case LifeTime.Singleton:
                        if (!_singletonCache.TryGetValue(key, out result))
                        {
                            result = _singletonCache.GetOrAdd(key, (str) =>
                            {
                                return new ImplementInstanceInfo()
                                {
                                    LifeTime = registerInfo.LifeTime,
                                    Instance = CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay)
                                };
                            });
                        }
                        return result.Instance as TInterface;
                    case LifeTime.Scoped:
                            IService oValue = CallContext.GetData(key);
                            if (oValue == null)
                            {
                                oValue = new ImplementInstanceInfo()
                                {
                                    LifeTime = registerInfo.LifeTime,
                                    Instance = CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay)
                                };
                                CallContext.SetData(key, oValue);
                            }
                            return oValue.Instance as TInterface;
                    case LifeTime.Transient:
                        return CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay) as TInterface;
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

        private string GetKey(string interfaceFullName, params object[] parameterValues)
        {
            if (parameterValues != null && parameterValues.Any())
            {
                foreach (object obj in parameterValues)
                {
                    interfaceFullName = $"{interfaceFullName}{obj.ToString()}";
                }
            }
            return interfaceFullName;
            //return System.Text.Encoding.UTF8.GetString(MD5.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(interfaceFullName)));
        }

        private void CheckIfExists(string key)
        {
            if (_container.ContainsKey(key))
            {
                throw new System.Exception($"This service:{key}-{_container[key]} exists in container, don't need to add again.");
            }
        }

        private object CreateObjectV2(Type type, object[] parameterValues = null, InjectionWay injectionWay = InjectionWay.None)
        {
            ConstructorInfo filteredConstructorInfo = null;
            MethodInfo filteredMethodInfo = null;
            IEnumerable<PropertyInfo> filteredPropertiesInfo = null;

            #region 默认走构造器注入方式
            if (injectionWay == InjectionWay.None)
            {
                object instance = null;
                //找参数最多的构造器
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null)
                {
                    filteredConstructorInfo = constructor;
                    //有参数就走有参数的
                    instance = CreateInstanceForConstructorInjection(type, filteredConstructorInfo, parameterValues);
                }
                else
                {
                    //一个构造器都没有是有问题的
                    throw new Evolution.IOC.Exception.InvalidDataException($"This type:{type.FullName} has no constructors.");
                }
                return instance;
            }
            #endregion

            #region 兼容模式注入,需要特性标识,才可注入成功
            if (injectionWay == InjectionWay.All)
            {
                object instance = null;
                //如果有构造器先把 instance建出来
                var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                if (constructor != null)
                {
                    instance = CreateInstanceForConstructorInjection(type, constructor);
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

            #region 指定某种注入方式
            if (injectionWay == (InjectionWay.Constructor))
            {
                var constructor = type.GetConstructors().Where(it => it.GetCustomAttribute<ConstructorInjectionAttribute>() != null).FirstOrDefault();
                return CreateInstanceForConstructorInjection(type, constructor, parameterValues);
            }

            if (injectionWay == (InjectionWay.Property))
            {
                var properties = type.GetProperties().Where(it => it.GetCustomAttribute<PropertyInjectionAttribute>() != null);
                if (properties != null && properties.Any())
                {
                    return CreateInstanceForPropertyInjection(type, properties, parameterValues);
                }
                return CreateDefaultInstance(type, parameterValues);
            }

            if (injectionWay == (InjectionWay.Method))
            {
                var method = type.GetMethods().Where(it => it.GetCustomAttribute<MethodInjectionAttribute>() != null).FirstOrDefault();
                if (method != null)
                {
                    return CreateInstanceForMethodInjection(type, method, parameterValues);
                }
                return CreateDefaultInstance(type, parameterValues);
            }

            #endregion

            #region 智能模式注入: 无需特性标识, 也可注入成功
            if (injectionWay == (InjectionWay.Smart))
            {
                object instance = null;
                //如果有构造器先把 instance建出来
                var constructor = GetConstructorWhichHasMostParametersCount(type);
                if (constructor != null && constructor.GetParameters().Length > 0)
                {
                    filteredConstructorInfo = constructor;
                    instance = CreateInstanceForConstructorInjection(type, filteredConstructorInfo);
                }

                IEnumerable<string> propertySetMethodNames = null;
                var properties = type.GetProperties().Where(it => it.PropertyType.IsInterface && it.GetGetMethod() != null && it.GetSetMethod() != null);
                if (properties != null && properties.Any())
                {
                    filteredPropertiesInfo = properties;
                    propertySetMethodNames = filteredPropertiesInfo.Select(it => it.GetSetMethod().Name);
                    if (instance != null)
                    {
                        SetInstanceProperties(instance, filteredPropertiesInfo);
                    }
                    else
                    {
                        instance = CreateInstanceForPropertyInjection(type, filteredPropertiesInfo, parameterValues);
                    }
                }

                
                var method = type.GetMethods().Where(it => it.IsStatic == false 
                && (   propertySetMethodNames!=null?!propertySetMethodNames.Contains(it.Name):true ) 
                && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface))
                    .OrderByDescending(it => it.GetParameters().Length)
                    .FirstOrDefault();
                if (method != null && method.GetParameters().Length > 0)
                {
                    filteredMethodInfo = method;
                    if (instance != null)
                    {
                        InvokeInstanceMethod(instance, filteredMethodInfo);
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
            object instance = CreateDefaultInstance(type, parameterValues);
            SetInstanceProperties(instance, properties);
            return instance;
        }

        private static object CreateDefaultInstance(Type type, object[] parameterValues)
        {
            if (parameterValues == null)
            {
                return CreateDefaultInstanceInternal(type);
            }
            else
            {
                var constructors = type.GetConstructors().Where(it=>it.GetParameters().Length == parameterValues.Length);
                if (constructors != null && constructors.Any())
                {
                    //构造器参数数量一致
                    var csts = constructors.ToList();
                    bool sameType = true;
                    for (var i = 0; i < parameterValues.Length; i++)
                    {
                        var queryParams = csts[i].GetParameters();
                        if (!CompareObjectType(parameterValues[i], queryParams[i]))
                        {
                            sameType = false;
                        }
                    }
                    //并且构造器参数类型一致
                    if (sameType)
                    {
                        return Activator.CreateInstance(type, parameterValues);
                    }
                }
                return CreateDefaultInstanceInternal(type);
            }
        }

        private static object CreateDefaultInstanceInternal(Type type)
        {
            var constructor = type.GetConstructors().FirstOrDefault();
            var parameters = constructor.GetParameters();
            if (parameters != null && parameters.Any())
            {
                List<object> pvs = new List<object>();
                foreach (var parameter in parameters)
                {
                    if (parameter.HasDefaultValue)
                    {
                        pvs.Add(parameter.DefaultValue);
                    }
                    else
                    {
                        pvs.Add(null);
                    }
                }
                return Activator.CreateInstance(type, pvs);
            }
            return Activator.CreateInstance(type, null);
        }

        /// <summary>
        /// CompareObjectType
        /// </summary>
        /// <param name="parameterValues"></param>
        /// <param name="i"></param>
        /// <param name="queryParams"></param>
        /// <returns>true:the same type, false:different type</returns>
        private static bool CompareObjectType(object obj, ParameterInfo parameterInfo)
        {
            if (obj == null && parameterInfo == null)
            {
                //一致
                return true;
            }
            else if (obj != null && parameterInfo != null)
            {
                return string.Equals(obj.GetType().FullName, parameterInfo.ParameterType.FullName, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                //肯定不一致
                return false;
            }
        }

        private void UpdateInstanceForPropertyInjection(Object instance, IEnumerable<PropertyInfo> properties, object[] parameterValues = null)
        {
            SetInstanceProperties(instance, properties);
        }

        private object CreateInstanceForConstructorInjection(Type type, ConstructorInfo constructor, object[] parameterValues = null)
        {
            if (constructor != null)
            {
                var interfaceParamValues = GetConstructorParamValuesFromIocContainer(constructor);
                if (interfaceParamValues != null && interfaceParamValues.Any())
                {
                    return Activator.CreateInstance(type, interfaceParamValues.ToArray());
                }
            }
            return CreateDefaultInstance(type, parameterValues);
        }

        private object CreateInstanceForMethodInjection(Type type, MethodInfo method, object[] parameterValues = null)
        {
            IList<object> interfaceParamValues = GetMethodParamValues(method);
            if (interfaceParamValues != null && interfaceParamValues.Any())
            {
                var instance = CreateDefaultInstance(type, parameterValues);
                method.Invoke(instance, interfaceParamValues.ToArray());
                return instance;
            }
            return CreateDefaultInstance(type, parameterValues);
        }

        private void InvokeInstanceMethod(object instance, MethodInfo method, object[] parameterValues = null)
        {
            IList<object> interfaceParamValues = GetMethodParamValues(method);
            if (interfaceParamValues != null && interfaceParamValues.Any())
            {
                method.Invoke(instance, interfaceParamValues.ToArray());
            }
        }
        
        private IList<object> GetConstructorParamValuesFromIocContainer(ConstructorInfo constructor)
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
                        object currentParamValue = CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
                        interfaceParamValues.Add(currentParamValue);
                    }
                    else
                    {
                        //构造器里可能有一些接口不是用来做IOC的参数，直接赋值成默认值
                        AddParameterDefaultTo(interfaceParamValues, parameter);
                    }
                }
                else
                {
                    //构造器里可能有一些不是用来做IOC的参数，直接赋值成默认值
                    AddParameterDefaultTo(interfaceParamValues, parameter);
                }
            }

            return interfaceParamValues;
        }

        private IList<object> GetConstructorParamDefaultValues(ConstructorInfo constructor)
        {
            IList<object> interfaceParamValues = new List<object>();
            foreach (var parameter in constructor.GetParameters())
            {
                AddParameterDefaultTo(interfaceParamValues, parameter);
            }
            return interfaceParamValues;
        }

        private static void AddParameterDefaultTo(IList<object> interfaceParamValues, ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                interfaceParamValues.Add(parameter.DefaultValue);
            }
            else
            {
                interfaceParamValues.Add(null);
            }
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
                        object currentParamValue = CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
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
                    var value = CreateObjectV2(registerInfo.ImplementClassType, null, registerInfo.InjectionWay);
                    interfaceProperty.SetValue(instance, value);
                }
            }
        }

        private object CreateInstanceByConstructor(Type type, ConstructorInfo constructor)
        {
            var interfaceParamValues = GetConstructorParamValuesFromIocContainer(constructor);
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

        private System.Reflection.ConstructorInfo GetConstructorWhichHasMostParametersCount(Type type)
        {
            return type.GetConstructors().OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
        }

        private System.Reflection.MethodInfo GetMethodWhichHasMostParametersCount(Type type)
        {
            return type.GetMethods().Where(it => it.IsStatic == false && it.GetParameters().Any(it1 => it1.ParameterType.IsInterface)).OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();//type.GetMethods().OrderByDescending(it => it.GetParameters().Length).FirstOrDefault();
        }

        private bool HasDefaultConstructor(Type type)
        {
            return type.GetConstructors().Any(it => it.GetParameters().Length == 0);
        }

        void ISyncConfig.Sync(IBaseServiceProvider serviceProvider)
        {
            IEnumerable<IRegisterInfo> services= serviceProvider.GetServices();
            foreach (var service in services)
            {
                _container.GetOrAdd(service.InterfaceType.FullName, service);
            }
        }

        void ISyncConfig.SyncJsonConfig(string jsonConfigLocalPath)
        {
            IBaseServiceProvider serviceProvider = new DefaultJsonServiceProvider(new FileInfo(jsonConfigLocalPath));
            Instance.Sync(serviceProvider);
        }

        void ISyncConfig.SyncXmlConfig(string xmlConfigLocalPath)
        {
            IBaseServiceProvider serviceProvider = new DefaultXmlServiceProvider(new FileInfo(xmlConfigLocalPath));
            Instance.Sync(serviceProvider);
            //Sync(serviceProvider);
        }

        public void Dispose()
        {
            lock (_lockObj)
            {
                _Instance = null;
                _container.Clear();
                CallContext.Clear();
            }
        }
    }
}
