using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Evolution.IOC.UnitTest
{
    class Program
    {
        private static readonly string xmlPath = GetFile(Directory.GetCurrentDirectory(), "IOC.xml");
        private static readonly string jsonPath = GetFile(Directory.GetCurrentDirectory(), "IOC.json");
        static void Main(string[] args)
        {
            //功能使用介绍
            FeaturesIntroduce();
        }

        private static void FeaturesIntroduce()
        {
            //1.支持依赖注入
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Simple case:");
                IRegisterInfo service = container.AddService<IMyService, MyService>();
                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            //2.支持生命周期控制,线程安全
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddSingleton:");
                //	2.1 进程级别 共享实例对象
                IRegisterInfo service = container.AddSingleton<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddScoped:");
                //	2.2 线程级别 共享实例对象 （用ThreadStatic特性控制：某一线程被反复利用，实例仍然是同一个）
                IRegisterInfo service = container.AddScoped<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddTransient:");
                //	2.3 每次都创建新对象
                IRegisterInfo service = container.AddTransient<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	2.4 自定义周期
                Console.WriteLine("Lifetime custom(this is Transient):");
                IRegisterInfo service = container.AddService<IMyService, MyService>(lifeTime: LifeTime.Transient);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            //3.支持配置文件和接口扩展（配置信息会Merge到IOCContainer中）
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.1 Xml，
                Console.WriteLine("IOC config for xml.");
                container.SyncXmlConfig(xmlPath);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.2 json， 
                Console.WriteLine("IOC config for json.");
                container.SyncJsonConfig(jsonPath);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.3 提供接口，以支持自定义扩展
                Console.WriteLine("IOC config for custom.");
                IBaseServiceProvider serviceProvider = new DefaultXmlServiceProvider(new FileInfo(xmlPath));
                container.Sync(serviceProvider);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            //4.支持多种注入模式：
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Constructor:");
                //	4.1 构造器注入（常规注入方式）： 遍历所有构造器，如果没有特性标识，默认找参数最多的构造器，进行依赖注入
                var service1 = container.AddScoped<IMyService, MyService_Constructor>();
                service1.UseInjectionWay(() => InjectionWay.Constructor);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Method:");
                //	4.2 方法注入（不推荐，和构造器注入原理一样，不过多出来一个无用的方法）： 遍历所有方法，如果没有特性标识，则不进行依赖注入
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Method>();
                service.UseInjectionWay(() => InjectionWay.Method);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Property:");
                //	4.3 属性注入（推荐）： 遍历所有属性，如果没有特性标识，则遍历所有的接口类型的属性，如果Container中有其注入，则赋值实现类
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Property>();
                service.UseInjectionWay(() => InjectionWay.Property);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Smart:");
                //	4.4 智能模式注入（傻瓜式）：不需要特性附着
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Smart>();
                service.UseInjectionWay(() => InjectionWay.Smart);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay All:");
                //	4.5 兼容模式注入（推荐）： 扫描所有附着的特性：构造器 / 属性 / 方法
                IRegisterInfo service = container.AddScoped<IMyService, MyService_CoverAll>();
                service.UseInjectionWay(() => InjectionWay.All);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
            }
        }

        private static void TestMultiThread(IContainer container)
        {
            int i = 0;
            IList<Task> tasks = new List<Task>();
            while (++i < 10)
            {
                var task = Task.Run(() =>
                {
                    var service1 = container.Resolve<IMyService>();
                    service1.Run();
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static string GetFile(string currectDirectoryPath, string fileName)
        {
            var directoryInfo = new DirectoryInfo(currectDirectoryPath);
            while (!directoryInfo.EnumerateFiles().Any(it => string.Equals(System.IO.Path.GetFileName(it.FullName), fileName, System.StringComparison.OrdinalIgnoreCase)))
            {
                if (directoryInfo.Parent == null) throw new FileNotFoundException($"Not found config:{fileName}");
                return GetFile(directoryInfo.Parent.FullName, fileName);
            }
            return $"{directoryInfo.FullName}\\{fileName}";
        }
    }
}
